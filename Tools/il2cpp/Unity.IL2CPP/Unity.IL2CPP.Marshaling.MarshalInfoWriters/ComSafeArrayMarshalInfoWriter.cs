using System;
using System.Collections.Generic;
using Mono.Cecil;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.Marshaling.MarshalInfoWriters
{
	public class ComSafeArrayMarshalInfoWriter : MarshalableMarshalInfoWriter
	{
		public enum Il2CppVariantType
		{
			None = 0,
			I2 = 2,
			I4 = 3,
			R4 = 4,
			R8 = 5,
			CY = 6,
			Date = 7,
			BStr = 8,
			Dispatch = 9,
			Error = 10,
			Bool = 11,
			Variant = 12,
			Unknown = 13,
			Decimal = 14,
			I1 = 16,
			UI1 = 17,
			UI2 = 18,
			UI4 = 19,
			I8 = 20,
			UI8 = 21,
			Int = 22,
			UInt = 23
		}

		private readonly TypeReference _elementType;

		private readonly SafeArrayMarshalInfo _marshalInfo;

		private readonly DefaultMarshalInfoWriter _elementTypeMarshalInfoWriter;

		private readonly MarshaledType[] _marshaledTypes;

		private readonly Il2CppVariantType _elementVariantType;

		public override MarshaledType[] MarshaledTypes => _marshaledTypes;

		public override string NativeSize => "-1";

		public ComSafeArrayMarshalInfoWriter(ReadOnlyContext context, ArrayType type, MarshalInfo marshalInfo)
			: base(context, type)
		{
			_elementType = type.ElementType;
			_marshalInfo = marshalInfo as SafeArrayMarshalInfo;
			_elementVariantType = GetElementVariantType(type.ElementType.MetadataType);
			if (_marshalInfo == null)
			{
				throw new InvalidOperationException($"SafeArray type '{type.FullName}' has invalid MarshalAsAttribute.");
			}
			if (_marshalInfo.ElementType == VariantType.BStr && _elementType.MetadataType != MetadataType.String)
			{
				throw new InvalidOperationException($"SafeArray(BSTR) type '{type.FullName}' has invalid MarshalAsAttribute.");
			}
			NativeType nativeElementType = GetNativeElementType();
			_elementTypeMarshalInfoWriter = MarshalDataCollector.MarshalInfoWriterFor(context, _elementType, MarshalType.COM, new MarshalInfo(nativeElementType));
			string text = $"Il2CppSafeArray/*{_marshalInfo.ElementType.ToString().ToUpper()}*/*";
			_marshaledTypes = new MarshaledType[1]
			{
				new MarshaledType(text, text)
			};
		}

		public ComSafeArrayMarshalInfoWriter(ReadOnlyContext context, ArrayType type)
			: this(context, type, new SafeArrayMarshalInfo())
		{
			_elementVariantType = GetElementVariantType(type.ElementType.MetadataType);
		}

		public static bool IsMarshalableAsSafeArray(ReadOnlyContext context, MetadataType metadataType)
		{
			if (metadataType != MetadataType.SByte && metadataType != MetadataType.Byte && metadataType != MetadataType.Int16 && metadataType != MetadataType.UInt16 && metadataType != MetadataType.Int32 && metadataType != MetadataType.UInt32 && metadataType != MetadataType.Int64 && metadataType != MetadataType.UInt64 && metadataType != MetadataType.Single && metadataType != MetadataType.Double && metadataType != MetadataType.IntPtr)
			{
				return metadataType == MetadataType.UIntPtr;
			}
			return true;
		}

		private static Il2CppVariantType GetElementVariantType(MetadataType metadataType)
		{
			switch (metadataType)
			{
			case MetadataType.Int16:
				return Il2CppVariantType.I2;
			case MetadataType.Int32:
				return Il2CppVariantType.I4;
			case MetadataType.Int64:
				return Il2CppVariantType.I8;
			case MetadataType.Single:
				return Il2CppVariantType.R4;
			case MetadataType.Double:
				return Il2CppVariantType.R8;
			case MetadataType.Byte:
				return Il2CppVariantType.I1;
			case MetadataType.SByte:
				return Il2CppVariantType.UI1;
			case MetadataType.UInt16:
				return Il2CppVariantType.UI2;
			case MetadataType.UInt32:
				return Il2CppVariantType.UI4;
			case MetadataType.UInt64:
				return Il2CppVariantType.UI8;
			case MetadataType.IntPtr:
				return Il2CppVariantType.Int;
			case MetadataType.UIntPtr:
				return Il2CppVariantType.UInt;
			case MetadataType.String:
				return Il2CppVariantType.BStr;
			default:
				throw new NotSupportedException($"SafeArray element type {metadataType} is not supported.");
			}
		}

		private NativeType GetNativeElementType()
		{
			switch (_elementVariantType)
			{
			case Il2CppVariantType.I2:
				return NativeType.I2;
			case Il2CppVariantType.I4:
				return NativeType.I4;
			case Il2CppVariantType.I8:
				return NativeType.I8;
			case Il2CppVariantType.R4:
				return NativeType.R4;
			case Il2CppVariantType.R8:
				return NativeType.R8;
			case Il2CppVariantType.BStr:
				return NativeType.BStr;
			case Il2CppVariantType.Dispatch:
				return NativeType.IDispatch;
			case Il2CppVariantType.Bool:
				return NativeType.VariantBool;
			case Il2CppVariantType.Unknown:
				return NativeType.IUnknown;
			case Il2CppVariantType.I1:
				return NativeType.I1;
			case Il2CppVariantType.UI1:
				return NativeType.U1;
			case Il2CppVariantType.UI2:
				return NativeType.U2;
			case Il2CppVariantType.UI4:
				return NativeType.U4;
			case Il2CppVariantType.UI8:
				return NativeType.U8;
			case Il2CppVariantType.Int:
				return NativeType.Int;
			case Il2CppVariantType.UInt:
				return NativeType.UInt;
			default:
				throw new NotSupportedException($"SafeArray element type {_elementVariantType} is not supported.");
			}
		}

		public override void WriteMarshaledTypeForwardDeclaration(IGeneratedCodeWriter writer)
		{
			_elementTypeMarshalInfoWriter.WriteMarshaledTypeForwardDeclaration(writer);
		}

		public override void WriteIncludesForFieldDeclaration(IGeneratedCodeWriter writer)
		{
			_elementTypeMarshalInfoWriter.WriteIncludesForFieldDeclaration(writer);
		}

		public override void WriteIncludesForMarshaling(IGeneratedMethodCodeWriter writer)
		{
			_elementTypeMarshalInfoWriter.WriteIncludesForMarshaling(writer);
			base.WriteIncludesForMarshaling(writer);
		}

		public override void WriteMarshalVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, string destinationVariable, string managedVariableName, IRuntimeMetadataAccess metadataAccess)
		{
			if (_marshalInfo.ElementType == VariantType.BStr)
			{
				writer.WriteLine("{0} = il2cpp_codegen_com_marshal_safe_array_bstring({1});", destinationVariable, sourceVariable.Load());
			}
			else
			{
				writer.WriteLine("{0} = il2cpp_codegen_com_marshal_safe_array(IL2CPP_VT_{1}, {2});", destinationVariable, _elementVariantType.ToString().ToUpper(), sourceVariable.Load());
			}
		}

		public override void WriteMarshalVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, ManagedMarshalValue destinationVariable, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, bool callConstructor, IRuntimeMetadataAccess metadataAccess)
		{
			if (_marshalInfo.ElementType == VariantType.BStr)
			{
				writer.WriteLine(destinationVariable.Store("({0}*)il2cpp_codegen_com_marshal_safe_array_bstring_result({1}, {2})", _context.Global.Services.Naming.ForType(_typeRef), metadataAccess.TypeInfoFor(_elementType), variableName));
			}
			else
			{
				writer.WriteLine(destinationVariable.Store("({0}*)il2cpp_codegen_com_marshal_safe_array_result(IL2CPP_VT_{1}, {2}, {3})", _context.Global.Services.Naming.ForType(_typeRef), _elementVariantType.ToString().ToUpper(), metadataAccess.TypeInfoFor(_elementType), variableName));
			}
		}

		public override void WriteMarshalCleanupVariable(IGeneratedMethodCodeWriter writer, string variableName, IRuntimeMetadataAccess metadataAccess, string managedVariableName = null)
		{
			writer.WriteLine("il2cpp_codegen_com_destroy_safe_array({0});", variableName);
			writer.WriteLine("{0} = {1};", variableName, "NULL");
		}
	}
}
