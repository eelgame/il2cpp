using System.Collections.Generic;
using Mono.Cecil;
using Unity.Cecil.Awesome;
using Unity.Cecil.Awesome.Comparers;
using Unity.IL2CPP.Contexts;

namespace Unity.IL2CPP
{
	internal static class IsManagedIntrinsics
	{
		public static bool IsUnmangedCall(ReadOnlyContext context, MethodReference method)
		{
			if (!context.Global.Parameters.UsingTinyClassLibraries)
			{
				return false;
			}
			if (method.DeclaringType == null || method.DeclaringType.Name != "UnsafeUtility" || method.Name != "IsUnmanaged")
			{
				return false;
			}
			if (method.ReturnType == null || method.ReturnType.FullName != "System.Boolean")
			{
				return false;
			}
			if (!method.IsGenericInstance)
			{
				return false;
			}
			GenericInstanceMethod genericInstanceMethod = method as GenericInstanceMethod;
			if (!genericInstanceMethod.HasGenericArguments)
			{
				return false;
			}
			if (genericInstanceMethod.GenericArguments.Count != 1)
			{
				return false;
			}
			return true;
		}

		public static bool IsManagedCall(MethodReference method)
		{
			if (method.DeclaringType == null || method.DeclaringType.Name != "RuntimeHelpers" || method.Name != "IsReferenceOrContainsReferences")
			{
				return false;
			}
			if (method.ReturnType == null || method.ReturnType.FullName != "System.Boolean")
			{
				return false;
			}
			if (!method.IsGenericInstance)
			{
				return false;
			}
			GenericInstanceMethod genericInstanceMethod = method as GenericInstanceMethod;
			if (!genericInstanceMethod.HasGenericArguments)
			{
				return false;
			}
			if (genericInstanceMethod.GenericArguments.Count != 1)
			{
				return false;
			}
			return true;
		}

		private static bool TypeHasReferences(TypeReference type, HashSet<TypeReference> seenTypes)
		{
			if (seenTypes.Contains(type))
			{
				return false;
			}
			seenTypes.Add(type);
			if (type.IsPointer)
			{
				return false;
			}
			if (!type.IsValueType())
			{
				return true;
			}
			TypeDefinition typeDefinition = type.Resolve();
			if (typeDefinition != null && typeDefinition.HasFields)
			{
				foreach (FieldDefinition field in typeDefinition.Fields)
				{
					if (!field.IsStatic)
					{
						TypeReference type2;
						if (field.FieldType.IsGenericParameter)
						{
							GenericParameter genericParameter = field.FieldType as GenericParameter;
							type2 = (type as GenericInstanceType).GenericArguments[genericParameter.Position];
						}
						else
						{
							type2 = field.FieldType;
						}
						if (TypeHasReferences(type2, seenTypes))
						{
							return true;
						}
					}
				}
			}
			return false;
		}

		public static string IsArgumentTypeUnmanaged(MethodReference isUnmanagedMethod)
		{
			TypeReference type = (isUnmanagedMethod as GenericInstanceMethod).GenericArguments[0];
			HashSet<TypeReference> seenTypes = new HashSet<TypeReference>(new TypeReferenceEqualityComparer());
			if (!TypeHasReferences(type, seenTypes))
			{
				return "true";
			}
			return "false";
		}

		public static string IsArgumentTypeManaged(MethodReference isManagedCall)
		{
			TypeReference type = (isManagedCall as GenericInstanceMethod).GenericArguments[0];
			HashSet<TypeReference> seenTypes = new HashSet<TypeReference>(new TypeReferenceEqualityComparer());
			if (!TypeHasReferences(type, seenTypes))
			{
				return "false";
			}
			return "true";
		}
	}
}
