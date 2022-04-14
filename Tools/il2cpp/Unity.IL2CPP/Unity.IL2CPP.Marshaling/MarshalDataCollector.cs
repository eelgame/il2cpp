using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Unity.Cecil.Awesome;
using Unity.Cecil.Awesome.Comparers;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Marshaling.MarshalInfoWriters;

namespace Unity.IL2CPP.Marshaling
{
	public class MarshalDataCollector
	{
		public static DefaultMarshalInfoWriter MarshalInfoWriterFor(ReadOnlyContext context, TypeReference type, MarshalType marshalType, MarshalInfo marshalInfo = null, bool useUnicodeCharSet = false, bool forByReferenceType = false, bool forFieldMarshaling = false, bool forReturnValue = false, bool forNativeToManagedWrapper = false, HashSet<TypeReference> typesForRecursiveFields = null)
		{
			type = type.WithoutModifiers();
			if (type is TypeSpecification && !(type is ArrayType) && !(type is ByReferenceType) && !(type is PointerType) && !(type is GenericInstanceType))
			{
				return new UnmarshalableMarshalInfoWriter(context, type);
			}
			if (type is GenericParameter || type.ContainsGenericParameters() || type.HasGenericParameters)
			{
				return new UnmarshalableMarshalInfoWriter(context, type);
			}
			return CreateMarshalInfoWriter(context, type, marshalType, marshalInfo, useUnicodeCharSet, forByReferenceType, forFieldMarshaling, forReturnValue, forNativeToManagedWrapper, typesForRecursiveFields);
		}

		private static DefaultMarshalInfoWriter CreateMarshalInfoWriter(ReadOnlyContext context, TypeReference type, MarshalType marshalType, MarshalInfo marshalInfo, bool useUnicodeCharSet, bool forByReferenceType, bool forFieldMarshaling, bool forReturnValue, bool forNativeToManagedWrapper, HashSet<TypeReference> typesForRecursiveFields)
		{
			return context.Global.Services.Factory.CreateMarshalInfoWriter(context, type, marshalType, marshalInfo, useUnicodeCharSet, forByReferenceType, forFieldMarshaling, forReturnValue, forNativeToManagedWrapper, typesForRecursiveFields);
		}

		public static bool HasCustomMarshalingMethods(TypeReference type, NativeType? nativeType, MarshalType marshalType, bool useUnicodeCharSet, bool forFieldMarshaling)
		{
			TypeDefinition typeDefinition = type.Resolve();
			if (typeDefinition.MetadataType != MetadataType.ValueType && typeDefinition.MetadataType != MetadataType.Class)
			{
				return false;
			}
			if (typeDefinition.HasGenericParameters && (forFieldMarshaling || typeDefinition.Fields.Any((FieldDefinition field) => field.FieldType.ContainsGenericParameters() || field.FieldType.IsGenericInstance)))
			{
				return false;
			}
			if (typeDefinition.IsInterface)
			{
				return false;
			}
			if (typeDefinition.MetadataType == MetadataType.ValueType && MarshalingUtils.IsBlittable(typeDefinition, nativeType, marshalType, useUnicodeCharSet))
			{
				return false;
			}
			if (marshalType == MarshalType.WindowsRuntime && typeDefinition.MetadataType != MetadataType.ValueType)
			{
				return false;
			}
			return typeDefinition.GetTypeHierarchy().All((TypeDefinition t) => t.IsSpecialSystemBaseType() || t.IsSequentialLayout || t.IsExplicitLayout);
		}

		public static bool FieldIsArrayOfType(FieldDefinition field, TypeReference typeRef)
		{
			if (field.FieldType is ArrayType arrayType)
			{
				return new TypeReferenceEqualityComparer().Equals(arrayType.ElementType, typeRef);
			}
			return false;
		}
	}
}
