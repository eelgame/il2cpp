using System.Collections.Generic;
using System.Collections.ObjectModel;
using Mono.Cecil;
using Unity.Cecil.Awesome;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Services;

namespace Unity.IL2CPP.StackAnalysis
{
	public static class StackAnalysisUtils
	{
		public delegate TypeReference ResultTypeAnalysisMethod(ReadOnlyContext context, TypeReference leftType, TypeReference rightType);

		private static readonly ReadOnlyCollection<MetadataType> _orderedTypes = new List<MetadataType>
		{
			MetadataType.Void,
			MetadataType.Boolean,
			MetadataType.Char,
			MetadataType.SByte,
			MetadataType.Byte,
			MetadataType.Int16,
			MetadataType.UInt16,
			MetadataType.Int32,
			MetadataType.UInt32,
			MetadataType.Int64,
			MetadataType.UInt64,
			MetadataType.Single,
			MetadataType.Double,
			MetadataType.String
		}.AsReadOnly();

		public static TypeReference GetWidestValueType(IEnumerable<TypeReference> types)
		{
			MetadataType value = _orderedTypes[0];
			TypeReference result = null;
			foreach (TypeReference type in types)
			{
				if (type.IsValueType() && !type.Resolve().IsEnum && _orderedTypes.IndexOf(type.MetadataType) > _orderedTypes.IndexOf(value))
				{
					value = type.MetadataType;
					result = type;
				}
			}
			return result;
		}

		public static TypeReference ResultTypeForAdd(ReadOnlyContext context, TypeReference leftType, TypeReference rightType)
		{
			return CorrectLargestTypeFor(context, leftType, rightType);
		}

		public static TypeReference ResultTypeForSub(ReadOnlyContext context, TypeReference leftType, TypeReference rightType)
		{
			if (leftType.MetadataType == MetadataType.Byte || leftType.MetadataType == MetadataType.UInt16)
			{
				return context.Global.Services.TypeProvider.Int32TypeReference;
			}
			if (leftType.MetadataType == MetadataType.Char)
			{
				return context.Global.Services.TypeProvider.Int32TypeReference;
			}
			return CorrectLargestTypeFor(context, leftType, rightType);
		}

		public static TypeReference ResultTypeForMul(ReadOnlyContext context, TypeReference leftType, TypeReference rightType)
		{
			return CorrectLargestTypeFor(context, leftType, rightType);
		}

		public static TypeReference CorrectLargestTypeFor(ReadOnlyContext context, TypeReference leftType, TypeReference rightType)
		{
			TypeReference typeReference = StackTypeConverter.StackTypeFor(context, leftType);
			TypeReference typeReference2 = StackTypeConverter.StackTypeFor(context, rightType);
			if (leftType.IsByReference)
			{
				return leftType;
			}
			if (rightType.IsByReference)
			{
				return rightType;
			}
			if (leftType.IsPointer)
			{
				return leftType;
			}
			if (rightType.IsPointer)
			{
				return rightType;
			}
			ITypeProviderService typeProvider = context.Global.Services.TypeProvider;
			if (typeReference.MetadataType == MetadataType.Int64 || typeReference2.MetadataType == MetadataType.Int64)
			{
				return typeProvider.Int64TypeReference;
			}
			if (typeReference.IsSameType(typeProvider.SystemIntPtr) || typeReference2.IsSameType(typeProvider.SystemIntPtr))
			{
				return typeProvider.SystemIntPtr;
			}
			if (typeReference.IsSameType(typeProvider.Int32TypeReference) && typeReference2.IsSameType(typeProvider.Int32TypeReference))
			{
				return typeProvider.Int32TypeReference;
			}
			return leftType;
		}
	}
}
