using System;
using Mono.Cecil;
using Unity.Cecil.Awesome;
using Unity.IL2CPP.Contexts;

namespace Unity.IL2CPP
{
	public class StackTypeConverter
	{
		public static TypeReference StackTypeFor(ReadOnlyContext context, TypeReference type)
		{
			if (type is PinnedType pinnedType)
			{
				type = pinnedType.ElementType;
			}
			if (type is ByReferenceType result)
			{
				return result;
			}
			if (type is RequiredModifierType requiredModifierType)
			{
				return StackTypeFor(context, requiredModifierType.ElementType);
			}
			if (type is OptionalModifierType optionalModifierType)
			{
				return StackTypeFor(context, optionalModifierType.ElementType);
			}
			if (type.IsSameType(context.Global.Services.TypeProvider.SystemIntPtr) || type.IsSameType(context.Global.Services.TypeProvider.SystemUIntPtr) || type.IsPointer)
			{
				return context.Global.Services.TypeProvider.SystemIntPtr;
			}
			if (!type.IsValueType())
			{
				return context.Global.Services.TypeProvider.ObjectTypeReference;
			}
			MetadataType metadataType = type.MetadataType;
			if (type.IsValueType() && type.IsEnum())
			{
				metadataType = type.GetUnderlyingEnumType().MetadataType;
			}
			switch (metadataType)
			{
			case MetadataType.Boolean:
			case MetadataType.Char:
			case MetadataType.SByte:
			case MetadataType.Byte:
			case MetadataType.Int16:
			case MetadataType.UInt16:
			case MetadataType.Int32:
			case MetadataType.UInt32:
				return context.Global.Services.TypeProvider.Int32TypeReference;
			case MetadataType.Int64:
			case MetadataType.UInt64:
				return context.Global.Services.TypeProvider.Int64TypeReference;
			case MetadataType.Single:
				return context.Global.Services.TypeProvider.SingleTypeReference;
			case MetadataType.Double:
				return context.Global.Services.TypeProvider.DoubleTypeReference;
			case MetadataType.IntPtr:
			case MetadataType.UIntPtr:
				return context.Global.Services.TypeProvider.SystemIntPtr;
			default:
				throw new ArgumentException($"Cannot get stack type for {type.Name}");
			}
		}

		public static string CppStackTypeFor(ReadOnlyContext context, TypeReference type)
		{
			TypeReference typeReference = StackTypeFor(context, type);
			if (typeReference.IsSameType(context.Global.Services.TypeProvider.SystemIntPtr) || typeReference.IsByReference)
			{
				return "intptr_t";
			}
			switch (typeReference.MetadataType)
			{
			case MetadataType.Int32:
				return "int32_t";
			case MetadataType.Int64:
				return "int64_t";
			case MetadataType.Double:
				return "double";
			case MetadataType.Single:
				return "float";
			default:
				throw new ArgumentException("Unexpected StackTypeFor: " + typeReference);
			}
		}

		public static TypeReference StackTypeForBinaryOperation(ReadOnlyContext context, TypeReference type)
		{
			TypeReference typeReference = StackTypeFor(context, type);
			if (typeReference is ByReferenceType)
			{
				return context.Global.Services.TypeProvider.SystemIntPtr;
			}
			return typeReference;
		}
	}
}
