using Mono.Cecil;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.Metadata.RuntimeTypes;

namespace Unity.IL2CPP.Naming
{
	public static class ArrayNaming
	{
		public static string ForRuntimeArrayType(this INamingService naming, Il2CppArrayRuntimeType type)
		{
			return naming.ForIl2CppType(type) + "_ArrayType";
		}

		public static string ForArrayItems()
		{
			return "m_Items";
		}

		public static string ForArrayItemGetter(bool useArrayBoundsCheck)
		{
			if (!useArrayBoundsCheck)
			{
				return "GetAtUnchecked";
			}
			return "GetAt";
		}

		public static string ForArrayItemAddressGetter(bool useArrayBoundsCheck)
		{
			if (!useArrayBoundsCheck)
			{
				return "GetAddressAtUnchecked";
			}
			return "GetAddressAt";
		}

		public static string ForArrayItemSetter(bool useArrayBoundsCheck)
		{
			if (!useArrayBoundsCheck)
			{
				return "SetAtUnchecked";
			}
			return "SetAt";
		}

		public static string ForArrayIndexType()
		{
			return "il2cpp_array_size_t";
		}

		public static string ForArrayIndexName()
		{
			return "index";
		}

		public static bool IsSpecialArrayMethod(MethodReference methodReference)
		{
			if (methodReference.Name == "Set" || methodReference.Name == "Get" || methodReference.Name == "Address" || methodReference.Name == ".ctor")
			{
				return methodReference.DeclaringType.IsArray;
			}
			return false;
		}
	}
}
