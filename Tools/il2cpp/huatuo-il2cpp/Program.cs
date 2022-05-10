using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using il2cpp.Conversion;
using Mono.Cecil;
using NiceIO;
using Unity.IL2CPP.AssemblyConversion.Phases;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.GenericsCollection;

namespace il2cpp
{
    public class Program
    {
        public static int Main(string[] args)
        {
            try
            {
                Il2CppOptionParser.ParseArguments(args, out var continueToRun, out var exitCode, out var platform,
                    out var buildingOptions, out var profilerOutput);

                var inputData = ContextDataFactory.CreateConversionDataFromOptions();

                using (var context = AssemblyConversionContext.SetupNew(
                           inputData,
                           ContextDataFactory.CreateConversionParametersFromOptions(),
                           ContextDataFactory.CreateTopLevelDataFromOptions()))
                {
                    InitializePhase.Run(context);
                    SetupPhase.Run(context, context.Results.Initialize.AllAssembliesOrderedByDependency);
                    var primaryCollectionContext = context.GlobalPrimaryCollectionContext.CreateCollectionContext();

                    var assemblyDefinitions = new List<AssemblyDefinition>();

                    foreach (var assemblyDefinition in context.Results.Initialize.AllAssembliesOrderedByDependency)
                        if (inputData.Assemblies.Contains(new NPath(assemblyDefinition.MainModule.FileName)))
                            assemblyDefinitions.Add(assemblyDefinition);

                    var inflatedCollectionCollector = GenericsCollector.Collect(primaryCollectionContext,
                        new ReadOnlyCollection<AssemblyDefinition>(assemblyDefinitions));
                    
                    Console.WriteLine("========================泛型扫描========================");
                    foreach (var type in inflatedCollectionCollector.AsReadOnly().Types)
                        if (type.Scope is ModuleDefinition md && !inputData.Assemblies.Contains(new NPath(md.FileName)))
                            if (Check(type))
                                Console.WriteLine(type);

                    Console.WriteLine("========================ValueType检测========================");
                    foreach (var type in inflatedCollectionCollector.AsReadOnly().Types)
                        if (!IsHotfixType(type.Scope, inputData.Assemblies))
                            foreach (var genericArgument in type.GenericArguments)
                                if (genericArgument.IsValueType &&
                                    IsHotfixType(genericArgument.Scope, inputData.Assemblies))
                                {
                                    
                                    Console.WriteLine(type);
                                    break;
                                }


                    return 0;
                }
            }
            catch (Exception arg)
            {
                Console.Error.WriteLine($"Unhandled exception: {arg}");
                return -1;
            }
        }

        private static bool IsHotfixType(IMetadataScope scope, ReadOnlyCollection<NPath> assemblies)
        {
            return scope is ModuleDefinition md && assemblies.Contains(md.FileName);
        }

        /// <summary>
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private static bool Check(GenericInstanceType type)
        {
            return true; // TODO 如果类型引用了热更dll则应该返回false，这里暂时没有处理
        }
    }
}