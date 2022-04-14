using System;
using System.Linq;
using Mono.Cecil;
using Unity.Cecil.Awesome;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.Metadata.RuntimeTypes;

namespace Unity.IL2CPP.Naming
{
	public static class TypeNaming
	{
		public static string ForType(this INamingService naming, TypeReference typeReference)
		{
			typeReference = typeReference.WithoutModifiers();
			return naming.ForTypeNameOnly(typeReference);
		}

		public static string ForRuntimeType(this INamingService naming, TypeReference typeReference)
		{
			typeReference = typeReference.WithoutModifiers();
			return naming.ForRuntimeUniqueTypeNameOnly(typeReference);
		}

		public static string ForTypeMangling(this INamingService naming, TypeReference typeReference)
		{
			if (typeReference.IsGenericParameter)
			{
				GenericParameter genericParameter = (GenericParameter)typeReference;
				return ((genericParameter.MetadataType == MetadataType.Var) ? "tgp" : "mgp") + genericParameter.Position;
			}
			if (typeReference.IsArray)
			{
				ArrayType arrayType = (ArrayType)typeReference;
				return naming.ForTypeMangling(arrayType.ElementType) + "_arr" + arrayType.Rank;
			}
			if (typeReference.IsGenericInstance)
			{
				GenericInstanceType genericInstanceType = (GenericInstanceType)typeReference;
				string text = naming.ForTypeMangling(genericInstanceType.ElementType) + "_git_";
				{
					foreach (TypeReference genericArgument in genericInstanceType.GenericArguments)
					{
						text = text + "_" + naming.ForTypeMangling(genericArgument);
					}
					return text;
				}
			}
			string text2 = naming.ForTypeNameOnly(typeReference);
			if (typeReference is ArrayType)
			{
				return text2 + "_arr";
			}
			if (typeReference is PointerType)
			{
				return text2 + "_ptr";
			}
			if (typeReference is ByReferenceType)
			{
				return text2 + "_ref";
			}
			return naming.Clean(text2);
		}

		public static string ForVariable(this INamingService naming, TypeReference variableType)
		{
			variableType = variableType.WithoutModifiers();
			ArrayType arrayType = variableType as ArrayType;
			PointerType pointerType = variableType as PointerType;
			ByReferenceType byReferenceType = variableType as ByReferenceType;
			if (arrayType != null)
			{
				int rank = arrayType.Rank;
				if (rank == 1)
				{
					return $"{naming.ForType(arrayType)}*";
				}
				if (rank > 1)
				{
					return $"{naming.ForType(arrayType)}*";
				}
				throw new NotImplementedException($"Invalid array rank {rank}");
			}
			if (pointerType != null)
			{
				return naming.ForVariable(pointerType.ElementType) + "*";
			}
			if (byReferenceType != null)
			{
				return naming.ForVariable(byReferenceType.ElementType) + "*";
			}
			switch (variableType.MetadataType)
			{
			case MetadataType.Void:
				return "void";
			case MetadataType.Boolean:
				return "bool";
			case MetadataType.Single:
				return "float";
			case MetadataType.Double:
				return "double";
			case MetadataType.String:
				return naming.ForType(variableType) + "*";
			case MetadataType.SByte:
				return "int8_t";
			case MetadataType.Byte:
				return "uint8_t";
			case MetadataType.Char:
				return "Il2CppChar";
			case MetadataType.Int16:
				return "int16_t";
			case MetadataType.UInt16:
				return "uint16_t";
			case MetadataType.Int32:
				return "int32_t";
			case MetadataType.UInt32:
				return "uint32_t";
			case MetadataType.Int64:
				return "int64_t";
			case MetadataType.UInt64:
				return "uint64_t";
			case MetadataType.IntPtr:
				return "intptr_t";
			case MetadataType.UIntPtr:
				return "uintptr_t";
			default:
			{
				if (variableType.Name == "intptr_t")
				{
					return "intptr_t";
				}
				if (variableType.Name == "uintptr_t")
				{
					return "uintptr_t";
				}
				if (variableType is GenericParameter)
				{
					throw new ArgumentException("Generic parameter encountered as variable type", "variableType");
				}
				TypeDefinition typeDefinition = variableType.Resolve();
				if (typeDefinition.IsEnum)
				{
					FieldDefinition fieldDefinition = typeDefinition.Fields.Single((FieldDefinition f) => f.Name == "value__");
					return naming.ForVariable(fieldDefinition.FieldType);
				}
				if (variableType is GenericInstanceType)
				{
					if (variableType.Resolve().IsInterface)
					{
						return naming.ForType(variableType.Module.TypeSystem.Object) + "*";
					}
					return string.Format("{0} {1}", naming.ForTypeNameOnly(variableType), NeedsAsterisk(variableType) ? "*" : string.Empty);
				}
				return ForVariableInternal(naming, variableType);
			}
			}
		}

		public static string ForIl2CppType(this INamingService naming, IIl2CppRuntimeType runtimeType)
		{
			TypeReference nonPinnedAndNonByReferenceType = runtimeType.Type.GetNonPinnedAndNonByReferenceType();
			string text = (nonPinnedAndNonByReferenceType.IsGenericParameter ? naming.ForGenericParameter((GenericParameter)nonPinnedAndNonByReferenceType) : ((runtimeType.Attrs == 0 && !(runtimeType.Type is TypeSpecification)) ? naming.ForTypeNameOnly(runtimeType.Type.WithoutModifiers()) : naming.ForRuntimeUniqueTypeNameOnly(runtimeType.Type.WithoutModifiers())));
			return text + "_" + (runtimeType.Type.IsByReference ? 1 : 0) + "_" + (runtimeType.Type.IsPinned ? 1 : 0) + "_" + runtimeType.Attrs;
		}

		public static string ForGenericParameter(this INamingService naming, GenericParameter genericParameter)
		{
			string arg;
			switch (genericParameter.Type)
			{
			case GenericParameterType.Type:
				arg = naming.ForTypeNameOnly((TypeReference)genericParameter.Owner);
				break;
			case GenericParameterType.Method:
				arg = naming.ForMethodNameOnly((MethodReference)genericParameter.Owner);
				break;
			default:
				throw new InvalidOperationException(string.Format("Unhandled {0} case {1}", "GenericParameterType", genericParameter.Owner.GenericParameterType));
			}
			return $"{arg}_gp_{genericParameter.Position}";
		}

		private static string ForVariableInternal(INamingService naming, TypeReference variableType)
		{
			if (variableType is RequiredModifierType requiredModifierType)
			{
				variableType = requiredModifierType.ElementType;
			}
			if (variableType is OptionalModifierType optionalModifierType)
			{
				variableType = optionalModifierType.ElementType;
			}
			if (variableType.Resolve().IsInterface)
			{
				return "RuntimeObject*";
			}
			return string.Format("{0} {1}", naming.ForType(variableType), NeedsAsterisk(variableType) ? "*" : string.Empty);
		}

		private static bool NeedsAsterisk(TypeReference type)
		{
			if (UnderlyingType(type).IsValueType())
			{
				return type.IsByReference;
			}
			return true;
		}

		private static TypeReference UnderlyingType(TypeReference type)
		{
			if (type is TypeSpecification typeSpecification)
			{
				return UnderlyingType(typeSpecification.ElementType);
			}
			return type;
		}
	}
}
