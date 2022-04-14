using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Unity.Cecil.Awesome;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Marshaling.MarshalInfoWriters;

namespace Unity.IL2CPP.Marshaling
{
	public static class MarshalingUtils
	{
		internal static bool IsBlittable(TypeReference type, NativeType? nativeType, MarshalType marshalType, bool useUnicodeCharset)
		{
			return IsBlittable(type, nativeType, marshalType, useUnicodeCharset, new HashSet<TypeDefinition>());
		}

		private static bool IsBlittable(TypeReference type, NativeType? nativeType, MarshalType marshalType, bool useUnicodeCharset, HashSet<TypeDefinition> previousTypes)
		{
			if (previousTypes.Contains(type))
			{
				return false;
			}
			if (type.ContainsGenericParameters() || (type is TypeSpecification && !type.IsGenericInstance))
			{
				return false;
			}
			if (type.IsPointer)
			{
				return true;
			}
			TypeDefinition typeDefinition = type.Resolve();
			useUnicodeCharset |= (typeDefinition.Attributes & TypeAttributes.UnicodeClass) != 0;
			if (typeDefinition.IsEnum)
			{
				return IsPrimitiveBlittable(type.GetUnderlyingEnumType().Resolve(), nativeType, marshalType, useUnicodeCharset);
			}
			if (typeDefinition.IsSequentialLayout || typeDefinition.IsExplicitLayout)
			{
				return AreFieldsBlittable(typeDefinition, nativeType, marshalType, useUnicodeCharset, previousTypes);
			}
			return false;
		}

		private static bool AreFieldsBlittable(TypeDefinition typeDef, NativeType? nativeType, MarshalType marshalType, bool useUnicodeCharset, HashSet<TypeDefinition> previousTypes)
		{
			while (typeDef != null)
			{
				if (typeDef.IsPrimitive)
				{
					return IsPrimitiveBlittable(typeDef, nativeType, marshalType, useUnicodeCharset);
				}
				foreach (FieldDefinition field in typeDef.Fields)
				{
					if (!field.IsStatic)
					{
						TypeReference fieldType = field.FieldType;
						if (fieldType.IsArray)
						{
							return false;
						}
						previousTypes.Add(typeDef);
						bool flag;
						try
						{
							TypeDefinition typeDefinition = fieldType.Resolve();
							flag = typeDefinition != null && IsBlittable(typeDefinition, GetFieldNativeType(field), marshalType, useUnicodeCharset, previousTypes);
						}
						finally
						{
							previousTypes.Remove(typeDef);
						}
						if (!flag)
						{
							return false;
						}
					}
				}
				TypeReference baseType = typeDef.BaseType;
				if (baseType == null)
				{
					break;
				}
				typeDef = baseType.Resolve();
			}
			return true;
		}

		private static NativeType? GetFieldNativeType(FieldDefinition field)
		{
			if (field.MarshalInfo == null)
			{
				return null;
			}
			if (field.MarshalInfo is ArrayMarshalInfo arrayMarshalInfo)
			{
				return arrayMarshalInfo.ElementType;
			}
			return field.MarshalInfo.NativeType;
		}

		private static bool IsPrimitiveBlittable(TypeDefinition type, NativeType? nativeType, MarshalType marshalType, bool useUnicodeCharset)
		{
			if (marshalType == MarshalType.ManagedLayout)
			{
				return true;
			}
			if (!nativeType.HasValue || nativeType == NativeType.Max)
			{
				if (marshalType == MarshalType.WindowsRuntime)
				{
					return true;
				}
				if (type.MetadataType == MetadataType.Char)
				{
					return useUnicodeCharset;
				}
				return type.MetadataType != MetadataType.Boolean;
			}
			switch (type.MetadataType)
			{
			case MetadataType.Boolean:
			case MetadataType.SByte:
			case MetadataType.Byte:
				if (nativeType != NativeType.U1)
				{
					return nativeType == NativeType.I1;
				}
				return true;
			case MetadataType.Char:
			case MetadataType.Int16:
			case MetadataType.UInt16:
				if (nativeType != NativeType.U2)
				{
					return nativeType == NativeType.I2;
				}
				return true;
			case MetadataType.Int32:
			case MetadataType.UInt32:
				if (nativeType != NativeType.U4)
				{
					return nativeType == NativeType.I4;
				}
				return true;
			case MetadataType.Int64:
			case MetadataType.UInt64:
				if (nativeType != NativeType.U8)
				{
					return nativeType == NativeType.I8;
				}
				return true;
			case MetadataType.IntPtr:
			case MetadataType.UIntPtr:
				if (nativeType != NativeType.UInt)
				{
					return nativeType == NativeType.Int;
				}
				return true;
			case MetadataType.Single:
				return nativeType == NativeType.R4;
			case MetadataType.Double:
				return nativeType == NativeType.R8;
			default:
				throw new ArgumentException($"{type.FullName} is not a primitive!");
			}
		}

		internal static bool IsStringBuilder(TypeReference type)
		{
			if (type.MetadataType == MetadataType.Class)
			{
				return type.FullName == "System.Text.StringBuilder";
			}
			return false;
		}

		internal static IEnumerable<FieldDefinition> NonStaticFieldsOf(TypeDefinition typeDefinition)
		{
			return typeDefinition.Fields.Where((FieldDefinition field) => !field.IsStatic);
		}

		internal static bool UseUnicodeAsDefaultMarshalingForStringParameters(MethodReference method)
		{
			MethodDefinition methodDefinition = method.Resolve();
			if (methodDefinition.HasPInvokeInfo)
			{
				if (!methodDefinition.PInvokeInfo.IsCharSetUnicode)
				{
					return methodDefinition.PInvokeInfo.IsCharSetAuto;
				}
				return true;
			}
			return false;
		}

		internal static bool UseUnicodeAsDefaultMarshalingForFields(TypeReference type)
		{
			TypeDefinition typeDefinition = type.Resolve();
			if (!typeDefinition.IsUnicodeClass)
			{
				return typeDefinition.IsAutoClass;
			}
			return true;
		}

		public static string MarshalTypeToString(MarshalType marshalType)
		{
			switch (marshalType)
			{
			case MarshalType.PInvoke:
				return "pinvoke";
			case MarshalType.COM:
				return "com";
			case MarshalType.WindowsRuntime:
				return "windows_runtime";
			default:
				throw new ArgumentException($"Unexpected MarshalType value '{marshalType}'.", "marshalType");
			}
		}

		public static string MarshalTypeToNiceString(MarshalType marshalType)
		{
			switch (marshalType)
			{
			case MarshalType.PInvoke:
				return "P/Invoke";
			case MarshalType.COM:
				return "COM";
			case MarshalType.WindowsRuntime:
				return "Windows Runtime";
			default:
				throw new ArgumentException($"Unexpected MarshalType value '{marshalType}'.", "marshalType");
			}
		}

		public static IEnumerable<FieldDefinition> GetMarshaledFields(ReadOnlyContext context, TypeDefinition type, MarshalType marshalType)
		{
			return (from t in type.GetTypeHierarchy()
				where t == type || MarshalDataCollector.MarshalInfoWriterFor(context, t, marshalType).HasNativeStructDefinition
				select t).SelectMany((TypeDefinition t) => NonStaticFieldsOf(t));
		}

		public static IEnumerable<DefaultMarshalInfoWriter> GetFieldMarshalInfoWriters(ReadOnlyContext context, TypeDefinition type, MarshalType marshalType)
		{
			return from f in GetMarshaledFields(context, type, marshalType)
				select MarshalDataCollector.MarshalInfoWriterFor(context, f.FieldType, marshalType, f.MarshalInfo, UseUnicodeAsDefaultMarshalingForFields(type), forByReferenceType: false, forFieldMarshaling: true);
		}

		public static MarshalType[] GetMarshalTypesForMarshaledType(ReadOnlyContext context, TypeReference type)
		{
			if (context.Global.Parameters.UsingTinyBackend)
			{
				return new MarshalType[1];
			}
			TypeReference typeReference = context.Global.Services.WindowsRuntime.ProjectToWindowsRuntime(type);
			if ((type.Resolve().IsExposedToWindowsRuntime() || typeReference != type) && (type.MetadataType == MetadataType.ValueType || typeReference.IsWindowsRuntimeDelegate(context)))
			{
				return new MarshalType[3]
				{
					MarshalType.PInvoke,
					MarshalType.COM,
					MarshalType.WindowsRuntime
				};
			}
			return new MarshalType[2]
			{
				MarshalType.PInvoke,
				MarshalType.COM
			};
		}

		public static bool IsMarshalableArrayField(FieldDefinition field)
		{
			if (!field.FieldType.IsArray)
			{
				return false;
			}
			if (field.MarshalInfo == null)
			{
				return true;
			}
			if (field.MarshalInfo.NativeType == NativeType.FixedArray || field.MarshalInfo.NativeType == NativeType.SafeArray)
			{
				return true;
			}
			TypeReference elementType = ((ArrayType)field.FieldType).ElementType;
			if (elementType.IsPrimitive || elementType.IsEnum())
			{
				return true;
			}
			return false;
		}

		public static bool HasMarshalableLayout(TypeReference type)
		{
			if (type.MetadataType == MetadataType.ValueType)
			{
				return true;
			}
			TypeDefinition typeDefinition = type.Resolve();
			if (typeDefinition != null)
			{
				if (!typeDefinition.IsSequentialLayout)
				{
					return typeDefinition.IsExplicitLayout;
				}
				return true;
			}
			return false;
		}

		public static bool IsMarshalable(ReadOnlyContext context, TypeReference type, MarshalType marshalType, MarshalInfo marshalInfo, bool useUnicodeCharSet, bool forFieldMarshaling, HashSet<TypeReference> previousTypes)
		{
			if (previousTypes.Contains(type))
			{
				return false;
			}
			DefaultMarshalInfoWriter defaultMarshalInfoWriter = MarshalDataCollector.MarshalInfoWriterFor(context, type, marshalType, marshalInfo, useUnicodeCharSet, forByReferenceType: false, forFieldMarshaling, forReturnValue: false, forNativeToManagedWrapper: false, previousTypes);
			if (defaultMarshalInfoWriter.CanMarshalTypeToNative())
			{
				return defaultMarshalInfoWriter.CanMarshalTypeFromNative();
			}
			return false;
		}
	}
}
