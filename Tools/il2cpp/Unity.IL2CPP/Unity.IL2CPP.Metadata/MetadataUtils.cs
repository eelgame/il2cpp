using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Unity.Cecil.Awesome;
using Unity.Cecil.Awesome.Comparers;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Results;
using Unity.IL2CPP.Metadata.RuntimeTypes;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.Metadata
{
	public class MetadataUtils
	{
		internal static string TypeRepositoryTypeFor(SourceWritingContext context, IIl2CppRuntimeType type)
		{
			return Emit.AddressOf(context.Global.Services.Naming.ForIl2CppType(type));
		}

		public static TypeReference GetUnderlyingType(TypeReference type)
		{
			if (type.IsEnum())
			{
				return type.GetUnderlyingEnumType();
			}
			return type;
		}

		private static byte[] GetBytesForConstantValue(object constantValueToSerialize, TypeReference declaredParameterOrFieldType, string name)
		{
			switch (declaredParameterOrFieldType.MetadataType)
			{
			case MetadataType.Boolean:
				return new byte[1] { (byte)(((bool)constantValueToSerialize) ? 1 : 0) };
			case MetadataType.Char:
				return BitConverter.GetBytes((ushort)(char)constantValueToSerialize);
			case MetadataType.SByte:
				return new byte[1] { (byte)(sbyte)constantValueToSerialize };
			case MetadataType.Byte:
				return new byte[1] { (byte)constantValueToSerialize };
			case MetadataType.Int16:
				return BitConverter.GetBytes((short)constantValueToSerialize);
			case MetadataType.UInt16:
				return BitConverter.GetBytes((ushort)constantValueToSerialize);
			case MetadataType.Int32:
				return BitConverter.GetBytes((int)constantValueToSerialize);
			case MetadataType.UInt32:
				return BitConverter.GetBytes((uint)constantValueToSerialize);
			case MetadataType.Int64:
				return BitConverter.GetBytes((long)constantValueToSerialize);
			case MetadataType.UInt64:
				return BitConverter.GetBytes((ulong)constantValueToSerialize);
			case MetadataType.Single:
				return BitConverter.GetBytes((float)constantValueToSerialize);
			case MetadataType.Double:
				return BitConverter.GetBytes((double)constantValueToSerialize);
			case MetadataType.Array:
			case MetadataType.Object:
				if (constantValueToSerialize != null)
				{
					throw new InvalidOperationException($"Default value for field {name} must be null.");
				}
				return null;
			case MetadataType.ByReference:
				return GetBytesForConstantValue(constantValueToSerialize, declaredParameterOrFieldType.GetElementType(), name);
			case MetadataType.String:
			{
				string text = (string)constantValueToSerialize;
				int byteCount = Encoding.UTF8.GetByteCount(text);
				byte[] array = new byte[4 + byteCount];
				Array.Copy(BitConverter.GetBytes(text.Length), array, 4);
				Array.Copy(Encoding.UTF8.GetBytes(text), 0, array, 4, byteCount);
				return array;
			}
			default:
				throw new ArgumentOutOfRangeException();
			}
		}

		internal static byte[] ConstantDataFor(ReadOnlyContext context, IConstantProvider constantProvider, TypeReference declaredParameterOrFieldType, string name)
		{
			if (declaredParameterOrFieldType is GenericInstanceType genericInstanceType && TypeReferenceEqualityComparer.AreEqual(declaredParameterOrFieldType.Resolve(), context.Global.Services.TypeProvider.SystemNullable))
			{
				return ConstantDataFor(context, constantProvider, genericInstanceType.GenericArguments[0], name);
			}
			if (declaredParameterOrFieldType.IsEnum())
			{
				declaredParameterOrFieldType = declaredParameterOrFieldType.GetUnderlyingEnumType();
			}
			object obj = constantProvider.Constant;
			if (DetermineMetadataTypeForDefaultValueBasedOnTypeOfConstant(declaredParameterOrFieldType.MetadataType, obj) != declaredParameterOrFieldType.MetadataType)
			{
				obj = ChangePrimitiveType(obj, declaredParameterOrFieldType);
			}
			return GetBytesForConstantValue(obj, declaredParameterOrFieldType, name);
		}

		private static MetadataType DetermineMetadataTypeForDefaultValueBasedOnTypeOfConstant(MetadataType metadataType, object constant)
		{
			if (constant is byte)
			{
				return MetadataType.Byte;
			}
			if (constant is sbyte)
			{
				return MetadataType.SByte;
			}
			if (constant is ushort)
			{
				return MetadataType.UInt16;
			}
			if (constant is short)
			{
				return MetadataType.Int16;
			}
			if (constant is uint)
			{
				return MetadataType.UInt32;
			}
			if (constant is int)
			{
				return MetadataType.Int32;
			}
			if (constant is ulong)
			{
				return MetadataType.UInt64;
			}
			if (constant is long)
			{
				return MetadataType.Int64;
			}
			if (constant is float)
			{
				return MetadataType.Single;
			}
			if (constant is double)
			{
				return MetadataType.Double;
			}
			if (constant is char)
			{
				return MetadataType.Char;
			}
			if (constant is bool)
			{
				return MetadataType.Boolean;
			}
			return metadataType;
		}

		public static object ChangePrimitiveType(object o, TypeReference type)
		{
			if (o is uint && type.MetadataType == MetadataType.Int32)
			{
				return (int)(uint)o;
			}
			if (o is int && type.MetadataType == MetadataType.UInt32)
			{
				return (uint)(int)o;
			}
			return Convert.ChangeType(o, DetermineTypeForDefaultValueBasedOnDeclaredType(type, o));
		}

		private static Type DetermineTypeForDefaultValueBasedOnDeclaredType(TypeReference type, object constant)
		{
			switch (type.MetadataType)
			{
			case MetadataType.Byte:
				return typeof(byte);
			case MetadataType.SByte:
				return typeof(sbyte);
			case MetadataType.UInt16:
				return typeof(ushort);
			case MetadataType.Int16:
				return typeof(short);
			case MetadataType.UInt32:
				return typeof(uint);
			case MetadataType.Int32:
				return typeof(int);
			case MetadataType.UInt64:
				return typeof(ulong);
			case MetadataType.Int64:
				return typeof(long);
			case MetadataType.Single:
				return typeof(float);
			case MetadataType.Double:
				return typeof(double);
			default:
				return constant.GetType();
			}
		}

		internal static bool TypesDoNotExceedMaximumRecursion(ReadOnlyContext context, IEnumerable<TypeReference> types)
		{
			return types.All((TypeReference t) => TypeDoesNotExceedMaximumRecursion(context, t));
		}

		internal static bool TypeDoesNotExceedMaximumRecursion(ReadOnlyContext context, TypeReference type)
		{
			if (type.IsGenericInstance)
			{
				return !GenericsUtilities.CheckForMaximumRecursion(context, (GenericInstanceType)type);
			}
			return true;
		}

		public static uint GetEncodedMethodMetadataUsageIndex(MethodReference method, IMetadataCollectionResults metadataCollection, IGenericMethodCollectorResults genericMethods)
		{
			if (method.IsGenericInstance || method.DeclaringType.IsGenericInstance)
			{
				if (genericMethods.TryGetValue(method, out var genericMethodIndex))
				{
					return GetEncodedMetadataUsageIndex(genericMethodIndex, Il2CppMetadataUsage.MethodRef);
				}
				return GetEncodedMetadataUsageIndex(0u, Il2CppMetadataUsage.Invalid);
			}
			return GetEncodedMetadataUsageIndex((uint)metadataCollection.GetMethodIndex(method.Resolve()), Il2CppMetadataUsage.MethodInfo);
		}

		internal static uint GetEncodedMetadataUsageIndex(uint index, Il2CppMetadataUsage type)
		{
			return (uint)((int)type << 29) | (index << 1) | 1u;
		}
	}
}
