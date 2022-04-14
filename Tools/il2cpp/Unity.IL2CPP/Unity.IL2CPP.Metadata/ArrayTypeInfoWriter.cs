using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Unity.IL2CPP.Contexts;

namespace Unity.IL2CPP.Metadata
{
	public class ArrayTypeInfoWriter
	{
		internal static IEnumerable<MethodReference> InflateArrayMethods(ReadOnlyContext context, ArrayType arrayType)
		{
			ModuleDefinition mainModule = context.Global.Services.TypeProvider.Corlib.MainModule;
			TypeDefinition type = mainModule.GetType("System.Array");
			TypeDefinition type2 = mainModule.GetType("System.Collections.Generic.ICollection`1");
			TypeDefinition type3 = mainModule.GetType("System.Collections.Generic.IList`1");
			TypeDefinition type4 = mainModule.GetType("System.Collections.Generic.IEnumerable`1");
			IEnumerable<MethodReference> first = Enumerable.Empty<MethodReference>().Concat(from m in GetArrayInterfaceMethods(type, type2, "InternalArray__ICollection_")
				select InflateArrayMethod(m, arrayType.ElementType)).Concat(from m in GetArrayInterfaceMethods(type, type3, "InternalArray__")
				select InflateArrayMethod(m, arrayType.ElementType))
				.Concat(from m in GetArrayInterfaceMethods(type, type4, "InternalArray__IEnumerable_")
					select InflateArrayMethod(m, arrayType.ElementType));
			TypeDefinition type5 = mainModule.GetType("System.Collections.Generic.IReadOnlyList`1");
			return Enumerable.Concat(second: from m in GetArrayInterfaceMethods(type, mainModule.GetType("System.Collections.Generic.IReadOnlyCollection`1"), "InternalArray__IReadOnlyCollection_")
				select InflateArrayMethod(m, arrayType.ElementType), first: first.Concat(from m in GetArrayInterfaceMethods(type, type5, "InternalArray__IReadOnlyList_")
				select InflateArrayMethod(m, arrayType.ElementType)));
		}

		internal static MethodReference InflateArrayMethod(MethodDefinition method, TypeReference elementType)
		{
			if (!method.HasGenericParameters)
			{
				return method;
			}
			return new GenericInstanceMethod(method)
			{
				GenericArguments = { elementType }
			};
		}

		internal static IEnumerable<MethodDefinition> GetArrayInterfaceMethods(TypeDefinition arrayType, TypeDefinition interfaceType, string arrayMethodPrefix)
		{
			if (interfaceType == null)
			{
				yield break;
			}
			foreach (MethodDefinition method in interfaceType.Methods)
			{
				string methodName = method.Name;
				MethodDefinition methodDefinition = arrayType.Methods.SingleOrDefault((MethodDefinition m) => m.Name.Length == arrayMethodPrefix.Length + methodName.Length && m.Name.StartsWith(arrayMethodPrefix) && m.Name.EndsWith(methodName));
				if (methodDefinition != null)
				{
					yield return methodDefinition;
				}
			}
		}

		internal static IEnumerable<TypeReference> TypeAndAllBaseAndInterfaceTypesFor(ReadOnlyContext context, TypeReference type)
		{
			List<TypeReference> list = new List<TypeReference>();
			while (type != null)
			{
				list.Add(type);
				foreach (TypeReference @interface in type.GetInterfaces(context))
				{
					if (!IsGenericInstanceWithMoreThanOneGenericArgument(@interface) && !IsSpecialCollectionGenericInterface(@interface.FullName))
					{
						list.Add(@interface);
					}
				}
				type = type.GetBaseType(context);
			}
			return list;
		}

		private static bool IsGenericInstanceWithMoreThanOneGenericArgument(TypeReference type)
		{
			if (type.IsGenericInstance && type is GenericInstanceType genericInstanceType && genericInstanceType.HasGenericArguments && genericInstanceType.GenericArguments.Count > 1)
			{
				return true;
			}
			return false;
		}

		private static bool IsSpecialCollectionGenericInterface(string typeFullName)
		{
			if (!typeFullName.Contains("System.Collections.Generic.ICollection`1") && !typeFullName.Contains("System.Collections.Generic.IEnumerable`1") && !typeFullName.Contains("System.Collections.Generic.IList`1") && !typeFullName.Contains("System.Collections.Generic.IReadOnlyList`1"))
			{
				return typeFullName.Contains("System.Collections.Generic.IReadOnlyCollection`1");
			}
			return true;
		}
	}
}
