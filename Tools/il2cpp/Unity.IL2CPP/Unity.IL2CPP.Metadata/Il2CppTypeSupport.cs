using System;
using Mono.Cecil;

namespace Unity.IL2CPP.Metadata
{
	internal static class Il2CppTypeSupport
	{
		internal static string DeclarationFor(TypeReference type)
		{
			switch (type.MetadataType)
			{
			case MetadataType.Void:
			case MetadataType.Boolean:
			case MetadataType.Char:
			case MetadataType.SByte:
			case MetadataType.Byte:
			case MetadataType.Int16:
			case MetadataType.UInt16:
			case MetadataType.Int32:
			case MetadataType.UInt32:
			case MetadataType.Int64:
			case MetadataType.UInt64:
			case MetadataType.Single:
			case MetadataType.Double:
			case MetadataType.String:
			case MetadataType.ValueType:
			case MetadataType.Class:
			case MetadataType.Var:
			case MetadataType.TypedByReference:
			case MetadataType.IntPtr:
			case MetadataType.UIntPtr:
			case MetadataType.Object:
			case MetadataType.MVar:
				return "Il2CppType";
			case MetadataType.ByReference:
				return DeclarationFor(((ByReferenceType)type).ElementType);
			case MetadataType.Pinned:
				return DeclarationFor(((PinnedType)type).ElementType);
			case MetadataType.RequiredModifier:
				return DeclarationFor(((RequiredModifierType)type).ElementType);
			case MetadataType.OptionalModifier:
				return DeclarationFor(((OptionalModifierType)type).ElementType);
			default:
				return "const Il2CppType";
			}
		}

		internal static string For(TypeReference type)
		{
			switch (type.MetadataType)
			{
			case MetadataType.Void:
				return "IL2CPP_TYPE_VOID";
			case MetadataType.Boolean:
				return "IL2CPP_TYPE_BOOLEAN";
			case MetadataType.Char:
				return "IL2CPP_TYPE_CHAR";
			case MetadataType.SByte:
				return "IL2CPP_TYPE_I1";
			case MetadataType.Byte:
				return "IL2CPP_TYPE_U1";
			case MetadataType.Int16:
				return "IL2CPP_TYPE_I2";
			case MetadataType.UInt16:
				return "IL2CPP_TYPE_U2";
			case MetadataType.Int32:
				return "IL2CPP_TYPE_I4";
			case MetadataType.UInt32:
				return "IL2CPP_TYPE_U4";
			case MetadataType.Int64:
				return "IL2CPP_TYPE_I8";
			case MetadataType.UInt64:
				return "IL2CPP_TYPE_U8";
			case MetadataType.Single:
				return "IL2CPP_TYPE_R4";
			case MetadataType.Double:
				return "IL2CPP_TYPE_R8";
			case MetadataType.String:
				return "IL2CPP_TYPE_STRING";
			case MetadataType.Pointer:
				return "IL2CPP_TYPE_PTR";
			case MetadataType.ByReference:
				return For(((ByReferenceType)type).ElementType);
			case MetadataType.ValueType:
				return "IL2CPP_TYPE_VALUETYPE";
			case MetadataType.Class:
				if (type.Resolve().MetadataType == MetadataType.ValueType)
				{
					return "IL2CPP_TYPE_VALUETYPE";
				}
				return "IL2CPP_TYPE_CLASS";
			case MetadataType.Array:
				if (((ArrayType)type).IsVector)
				{
					return "IL2CPP_TYPE_SZARRAY";
				}
				return "IL2CPP_TYPE_ARRAY";
			case MetadataType.GenericInstance:
				return "IL2CPP_TYPE_GENERICINST";
			case MetadataType.TypedByReference:
				return "IL2CPP_TYPE_TYPEDBYREF";
			case MetadataType.IntPtr:
				return "IL2CPP_TYPE_I";
			case MetadataType.UIntPtr:
				return "IL2CPP_TYPE_U";
			case MetadataType.FunctionPointer:
				throw new ArgumentOutOfRangeException();
			case MetadataType.Object:
				return "IL2CPP_TYPE_OBJECT";
			case MetadataType.Var:
				return "IL2CPP_TYPE_VAR";
			case MetadataType.MVar:
				return "IL2CPP_TYPE_MVAR";
			case MetadataType.RequiredModifier:
				return For(((RequiredModifierType)type).ElementType);
			case MetadataType.OptionalModifier:
				return For(((OptionalModifierType)type).ElementType);
			case MetadataType.Sentinel:
				throw new ArgumentOutOfRangeException();
			case MetadataType.Pinned:
				throw new ArgumentOutOfRangeException();
			default:
				throw new ArgumentOutOfRangeException();
			}
		}
	}
}
