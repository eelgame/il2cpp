// Copyright (c) Petabridge <https://petabridge.com/>. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using NBench.Reporting;
using NBench.Sdk.Compiler.Assemblies;

#if CORECLR
using Microsoft.Extensions.DependencyModel;
#endif

namespace NBench.Sdk.Compiler
{
    public partial class ReflectionDiscovery
    {
        public static readonly Type MeasurementConfiguratorType = typeof(IMeasurementConfigurator<>);

        /// <summary>
        /// Cache of <see cref="MeasurementAttribute"/> types and their "best fitting" <see cref="IMeasurementConfigurator"/> type
        /// </summary>
        private readonly ConcurrentDictionary<Type, Type> _measurementConfiguratorTypes = new ConcurrentDictionary<Type, Type>();

        /// <summary>
        /// Finds a matching <see cref="IMeasurementConfigurator{T}"/> type for a given type of <see cref="MeasurementAttribute"/>
        /// </summary>
        /// <param name="measurementType">A type of <see cref="MeasurementAttribute"/></param>
        /// <param name="specificAssembly">
        ///     Optional parameter. If an <see cref="Assembly"/> is provided, we limit our search 
        ///     for <see cref="IMeasurementConfigurator{T}"/> definitions to just that target assembly.
        /// </param>
        /// <returns>A corresponding <see cref="IMeasurementConfigurator{T}"/> type</returns>
        public Type GetConfiguratorTypeForMeasurement(Type measurementType, IAssemblyLoader specificAssembly = null)
        {
            ValidateTypeIsMeasurementAttribute(measurementType);

            // served up the cached version if we already have it
            if (_measurementConfiguratorTypes.ContainsKey(measurementType))
                return _measurementConfiguratorTypes[measurementType];

            using (specificAssembly = specificAssembly ??
                                      AssemblyRuntimeLoader.WrapAssembly(measurementType.Assembly, Output))
            {

                // search for a match
                var match = FindBestMatchingConfiguratorForMeasurement(measurementType,
                    LoadAllTypeConfigurators(specificAssembly));

                // cache the result
                _measurementConfiguratorTypes[measurementType] = match;

                return match;
            }
        }

        /// <summary>
        /// Creates a <see cref="IMeasurementConfigurator"/> instance for the provided <see cref="MeasurementAttribute"/> type.
        /// </summary>
        /// <param name="measurementType">A type of <see cref="MeasurementAttribute"/></param>
        /// <param name="specificAssembly">
        ///     Optional parameter. If an <see cref="Assembly"/> is provided, we limit our search 
        ///     for <see cref="IMeasurementConfigurator{T}"/> definitions to just that target assembly.
        /// </param>
        /// <returns>
        ///     If a <see cref="IMeasurementConfigurator"/> type match was found, this method will return a NEW instance of that.
        ///     If no match was found, we return a special case instance of <see cref="MeasurementConfigurator.EmptyConfigurator"/>.
        /// </returns>
        public IMeasurementConfigurator GetConfiguratorForMeasurement(Type measurementType, IAssemblyLoader specificAssembly = null)
        {
            ValidateTypeIsMeasurementAttribute(measurementType);

            var configuratorType = GetConfiguratorTypeForMeasurement(measurementType, specificAssembly);

            // special case: EmptyConfigurator (no match found)
            if (configuratorType == MeasurementConfigurator.EmptyConfiguratorType)
                return MeasurementConfigurator.EmptyConfigurator.Instance;

            // construct the instance
            return (IMeasurementConfigurator)Activator.CreateInstance(configuratorType);

        }

        private static readonly IReadOnlyList<Type> NoTypes = new Type[0];

        public static IEnumerable<Type> LoadAllTypeConfigurators(Assembly assembly, IBenchmarkOutput output = null)
        {
            using (var loader = AssemblyRuntimeLoader.WrapAssembly(assembly, output))
            {
                return LoadAllTypeConfigurators(loader);
            }
        }

        public static IEnumerable<Type> LoadAllTypeConfigurators(IAssemblyLoader loader)
        {
            var types = loader.AssemblyAndDependencies.SelectMany(a => a.DefinedTypes).Distinct().Select(x => x.AsType());
            return
                types.Where(IsConfigurationType);
        }

        public static Type FindBestMatchingConfiguratorForMeasurement(Type measurementType,
            IEnumerable<Type> knownConfigurators)
        {
            IEnumerable<Type> seed = new List<Type>();
            var configuratorsThatSupportType = knownConfigurators.Aggregate(seed,
                (list, type) =>
                {
                    var supportsType = GetConfiguratorInterfaces(type).Any<Type>(y => SupportsType(measurementType, y, false));
                    return supportsType ? list.Concat(new[] { type }) : list;
                }).ToList();

            // short-circuit if we couldn't find any matching types
            if (!configuratorsThatSupportType.Any())
                return MeasurementConfigurator.EmptyConfiguratorType;

            var currentType = measurementType;

            // check base classes first
            while (currentType != typeof(object) && currentType != null)
            {
                foreach (var configurator in configuratorsThatSupportType)
                {
                    if (ConfiguratorSupportsMeasurement(currentType, configurator, true))
                        return configurator;
                }
                currentType = currentType.GetTypeInfo().BaseType; //descend down the inheritance chain
            }

            // check interfaces next
            var interfaces = measurementType.GetTypeInfo().ImplementedInterfaces;
            foreach (var i in interfaces)
            {
                foreach (var configurator in configuratorsThatSupportType)
                {
                    if (ConfiguratorSupportsMeasurement(measurementType, configurator, true))
                        return configurator;
                }
            }

            throw new InvalidOperationException(
                "Code never should have reached this line. Should have found matching type");
        }

        /// <summary>
        /// Determine if a given <see cref="IMeasurementConfigurator"/> type is a match for a
        /// <see cref="MeasurementAttribute"/> type.
        /// </summary>
        /// <param name="measurementType">A <see cref="MeasurementAttribute"/> type.</param>
        /// <param name="expectedConfiguratorType">A <see cref="IMeasurementConfigurator"/> type.</param>
        /// <param name="exact">
        ///     If <c>true</c>, then this method will look for an exact 1:1 type match. 
        ///     If <c>false</c>, which is the default then this method will return <c>true</c>
        ///     when any applicable types are assignable from <paramref name="measurementType"/>.
        /// </param>
        /// <returns><c>true</c> if a match was found, <c>false</c> otherwise.</returns>
        public static bool ConfiguratorSupportsMeasurement(Type measurementType,
            Type expectedConfiguratorType, bool exact = false)
        {
            ValidateTypeIsMeasurementAttribute(measurementType);
            Contract.Assert(IsValidConfiguratorType(expectedConfiguratorType), $"{expectedConfiguratorType} must derive from {MeasurementConfiguratorType}");

            var currentType = expectedConfiguratorType;
            while (currentType != typeof(object) && currentType != null)
            {
                var configurators = GetConfiguratorInterfaces(currentType).ToList<Type>();
                if (configurators.Count == 0)
                {
                    currentType = currentType.GetTypeInfo().BaseType;
                    continue; //move down the inheritance chain
                }

                // otherwise, we need to scan each of the IMeasurementConfigurator<> matches and make sure that the
                // type arguments are assignable
                return configurators.Any(configuratorType => SupportsType(measurementType, configuratorType, exact));
            }

            return false;

        }

        private static bool IsConfigurationType(Type type)
        {
            var tpInfo = type.GetTypeInfo();

            return tpInfo.IsClass && !tpInfo.IsGenericTypeDefinition &&
                   IsValidConfiguratorType(type);
        }

        private static bool SupportsType(Type measurementType, Type configuratorType, bool exact)
        {
            return configuratorType.GenericTypeArguments.Any(y => exact ? y == measurementType : y.IsAssignableFrom(measurementType));
        }

        /// <summary>
        /// Check if a given <paramref name="configuratorType"/> is a valid implementation of <see cref="IMeasurementConfigurator{T}"/>.
        /// </summary>
        /// <param name="configuratorType">The <see cref="Type"/> we're going to test.</param>
        /// <returns>true if <paramref name="configuratorType"/> implements <see cref="IMeasurementConfigurator{T}"/>, false otherwise.</returns>
        public static bool IsValidConfiguratorType(Type configuratorType)
        {
            return GetConfiguratorInterfaces(configuratorType).Any();
        }

        private static IEnumerable<Type> GetConfiguratorInterfaces(Type type)
        {
            var genericInterfaceDefinitions = type.GetTypeInfo().ImplementedInterfaces.Where(x => x.GetTypeInfo().IsGenericType);
            return genericInterfaceDefinitions.Where(x => x.GetGenericTypeDefinition() == MeasurementConfiguratorType);
        }

        private static void ValidateTypeIsMeasurementAttribute(Type measurementType)
        {
            Contract.Requires(measurementType != null);
            Contract.Assert(MeasurementAttributeType.IsAssignableFrom(measurementType),
                $"{measurementType} must derive from {MeasurementAttributeType}");
        }
    }
}

