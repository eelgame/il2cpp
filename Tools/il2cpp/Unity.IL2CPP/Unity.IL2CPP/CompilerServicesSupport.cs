using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Mono.Cecil;
using Mono.Collections.Generic;
using Unity.IL2CPP.CompilerServices;
using Unity.IL2CPP.Contexts;

namespace Unity.IL2CPP
{
	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct CompilerServicesSupport
	{
		private const string SetOptionsAttributeFullName = "Unity.IL2CPP.CompilerServices.Il2CppSetOptionAttribute";

		public static bool HasNullChecksSupportEnabled(ReadOnlyContext context, MethodDefinition methodDefinition, bool globalValue)
		{
			if (context.Global.Parameters.UsingTinyBackend)
			{
				return false;
			}
			return HasOptionEnabled(methodDefinition, Option.NullChecks, globalValue);
		}

		public static bool HasArrayBoundsChecksSupportEnabled(MethodDefinition methodDefinition, bool globalValue)
		{
			return HasOptionEnabled(methodDefinition, Option.ArrayBoundsChecks, globalValue);
		}

		public static bool HasDivideByZeroChecksSupportEnabled(MethodDefinition methodDefinition, bool globalValue)
		{
			return HasOptionEnabled(methodDefinition, Option.DivideByZeroChecks, globalValue);
		}

		private static bool IsEagerStaticClassConstructionAttribute(CustomAttribute ca)
		{
			TypeReference attributeType = ca.AttributeType;
			if (!(attributeType.Namespace == "Unity.IL2CPP.CompilerServices") || !(attributeType.Name == "Il2CppEagerStaticClassConstructionAttribute"))
			{
				if (attributeType.Namespace == "System.Runtime.CompilerServices")
				{
					return attributeType.Name == "EagerStaticClassConstructionAttribute";
				}
				return false;
			}
			return true;
		}

		public static bool HasEagerStaticClassConstructionEnabled(TypeDefinition type)
		{
			if (!type.HasGenericParameters && type.HasCustomAttributes)
			{
				return type.CustomAttributes.Any(IsEagerStaticClassConstructionAttribute);
			}
			return false;
		}

		public static bool IsSetOptionAttribute(CustomAttribute attribute)
		{
			return attribute.AttributeType.FullName == "Unity.IL2CPP.CompilerServices.Il2CppSetOptionAttribute";
		}

		private static bool HasOptionEnabled(IMemberDefinition methodDefinition, Option option, bool globalValue)
		{
			bool result = globalValue;
			if (GetBooleanOptionValue(methodDefinition.CustomAttributes, option, ref result))
			{
				return result;
			}
			TypeDefinition declaringType = methodDefinition.DeclaringType;
			foreach (PropertyDefinition property in declaringType.Properties)
			{
				if ((property.GetMethod == methodDefinition || property.SetMethod == methodDefinition) && GetBooleanOptionValue(property.CustomAttributes, option, ref result))
				{
					return result;
				}
			}
			if (GetBooleanOptionValue(declaringType.CustomAttributes, option, ref result))
			{
				return result;
			}
			return globalValue;
		}

		private static bool GetBooleanOptionValue(IEnumerable<CustomAttribute> attributes, Option option, ref bool result)
		{
			return GetOptionValue(attributes, option, ref result);
		}

		private static bool GetOptionValue<T>(IEnumerable<CustomAttribute> attributes, Option option, ref T result)
		{
			foreach (CustomAttribute attribute in attributes)
			{
				if (!IsSetOptionAttribute(attribute))
				{
					continue;
				}
				Collection<CustomAttributeArgument> constructorArguments = attribute.ConstructorArguments;
				if ((int)constructorArguments[0].Value == (int)option)
				{
					try
					{
						result = (T)((CustomAttributeArgument)constructorArguments[1].Value).Value;
					}
					catch (InvalidCastException)
					{
						continue;
					}
					return true;
				}
			}
			return false;
		}
	}
}
