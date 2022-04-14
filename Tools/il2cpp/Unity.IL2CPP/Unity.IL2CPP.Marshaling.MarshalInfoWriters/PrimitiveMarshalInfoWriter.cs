using System.Collections.Generic;
using Mono.Cecil;
using Unity.Cecil.Awesome;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.Marshaling.MarshalInfoWriters
{
	internal sealed class PrimitiveMarshalInfoWriter : DefaultMarshalInfoWriter
	{
		private readonly int _nativeSizeWithoutPointers;

		private readonly string _nativeSize;

		private readonly string _marshaledTypeName;

		private readonly MarshaledType[] _marshaledTypes;

		public override int NativeSizeWithoutPointers => _nativeSizeWithoutPointers;

		public override string NativeSize => _nativeSize;

		public override MarshaledType[] MarshaledTypes => _marshaledTypes;

		public PrimitiveMarshalInfoWriter(ReadOnlyContext context, TypeReference type, MarshalInfo marshalInfo, MarshalType marshalType, bool useUnicodeCharSet = false)
			: base(context, type)
		{
			_marshaledTypeName = context.Global.Services.Naming.ForVariable(type);
			switch (type.MetadataType)
			{
			case MetadataType.Boolean:
				if (marshalType != MarshalType.WindowsRuntime)
				{
					_nativeSizeWithoutPointers = 4;
					_nativeSize = "4";
					_marshaledTypeName = "int32_t";
				}
				else
				{
					_nativeSizeWithoutPointers = 1;
					_nativeSize = "1";
					_marshaledTypeName = "bool";
				}
				break;
			case MetadataType.Char:
				if (marshalType == MarshalType.WindowsRuntime || useUnicodeCharSet)
				{
					_nativeSizeWithoutPointers = 2;
					_nativeSize = "2";
					_marshaledTypeName = "Il2CppChar";
				}
				else
				{
					_nativeSizeWithoutPointers = 1;
					_nativeSize = "1";
					_marshaledTypeName = "uint8_t";
				}
				break;
			case MetadataType.Void:
				_nativeSizeWithoutPointers = 1;
				_nativeSize = "1";
				break;
			case MetadataType.SByte:
			case MetadataType.Byte:
				_nativeSizeWithoutPointers = 1;
				break;
			case MetadataType.Int16:
			case MetadataType.UInt16:
				_nativeSizeWithoutPointers = 2;
				break;
			case MetadataType.Int32:
			case MetadataType.UInt32:
			case MetadataType.Single:
				_nativeSizeWithoutPointers = 4;
				break;
			case MetadataType.Int64:
			case MetadataType.UInt64:
			case MetadataType.Double:
				_nativeSizeWithoutPointers = 8;
				break;
			case MetadataType.IntPtr:
				_marshaledTypeName = "intptr_t";
				_nativeSizeWithoutPointers = 0;
				break;
			case MetadataType.UIntPtr:
				_marshaledTypeName = "uintptr_t";
				_nativeSizeWithoutPointers = 0;
				break;
			case MetadataType.Pointer:
				_nativeSizeWithoutPointers = 0;
				break;
			}
			if (marshalInfo != null)
			{
				switch (marshalInfo.NativeType)
				{
				case NativeType.Boolean:
				case NativeType.I4:
					_nativeSize = "4";
					_nativeSizeWithoutPointers = 4;
					_marshaledTypeName = "int32_t";
					break;
				case NativeType.I1:
					_nativeSize = "1";
					_nativeSizeWithoutPointers = 1;
					_marshaledTypeName = "int8_t";
					break;
				case NativeType.I2:
					_nativeSize = "2";
					_nativeSizeWithoutPointers = 2;
					_marshaledTypeName = "int16_t";
					break;
				case NativeType.I8:
					_nativeSize = "8";
					_nativeSizeWithoutPointers = 8;
					_marshaledTypeName = "int64_t";
					break;
				case NativeType.U1:
					_nativeSize = "1";
					_nativeSizeWithoutPointers = 1;
					_marshaledTypeName = "uint8_t";
					break;
				case NativeType.VariantBool:
					_nativeSize = "2";
					_nativeSizeWithoutPointers = 2;
					_marshaledTypeName = "IL2CPP_VARIANT_BOOL";
					break;
				case NativeType.U2:
					_nativeSize = "2";
					_nativeSizeWithoutPointers = 2;
					_marshaledTypeName = "uint16_t";
					break;
				case NativeType.U4:
					_nativeSize = "4";
					_nativeSizeWithoutPointers = 4;
					_marshaledTypeName = "uint32_t";
					break;
				case NativeType.U8:
					_nativeSize = "8";
					_nativeSizeWithoutPointers = 8;
					_marshaledTypeName = "uint64_t";
					break;
				case NativeType.R4:
					_nativeSize = "4";
					_nativeSizeWithoutPointers = 4;
					_marshaledTypeName = "float";
					break;
				case NativeType.R8:
					_nativeSize = "8";
					_nativeSizeWithoutPointers = 8;
					_marshaledTypeName = "double";
					break;
				case NativeType.Int:
					_nativeSize = "sizeof(void*)";
					_nativeSizeWithoutPointers = 0;
					_marshaledTypeName = "intptr_t";
					break;
				case NativeType.UInt:
					_nativeSize = "sizeof(void*)";
					_nativeSizeWithoutPointers = 0;
					_marshaledTypeName = "uintptr_t";
					break;
				}
			}
			_marshaledTypes = new MarshaledType[1]
			{
				new MarshaledType(_marshaledTypeName, _marshaledTypeName)
			};
			if (_nativeSize == null)
			{
				_nativeSize = base.NativeSize;
			}
		}

		public override void WriteMarshaledTypeForwardDeclaration(IGeneratedCodeWriter writer)
		{
			if (_typeRef.IsPointer && _typeRef.GetElementType().MetadataType == MetadataType.ValueType && !_typeRef.GetElementType().IsEnum())
			{
				string text = _context.Global.Services.Naming.ForVariable(_typeRef.GetElementType());
				writer.AddForwardDeclaration("struct " + text);
			}
		}

		public override void WriteIncludesForMarshaling(IGeneratedMethodCodeWriter writer)
		{
		}

		public override void WriteMarshalVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, string destinationVariable, string managedVariableName, IRuntimeMetadataAccess metadataAccess)
		{
			writer.WriteLine("{0} = {1};", destinationVariable, WriteMarshalVariableToNative(writer, sourceVariable, managedVariableName, metadataAccess));
		}

		public override string WriteMarshalVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, string managedVariableName, IRuntimeMetadataAccess metadataAccess)
		{
			if (_typeRef.MetadataType == MetadataType.Boolean && _marshaledTypeName == "IL2CPP_VARIANT_BOOL")
			{
				return MarshalVariantBoolToNative(sourceVariable.Load());
			}
			if (_context.Global.Services.Naming.ForVariable(_typeRef) != _marshaledTypeName)
			{
				return $"static_cast<{_marshaledTypeName}>({sourceVariable.Load()})";
			}
			return sourceVariable.Load();
		}

		public override string WriteMarshalVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, IRuntimeMetadataAccess metadataAccess)
		{
			if (_typeRef.MetadataType == MetadataType.Boolean && _marshaledTypeName == "IL2CPP_VARIANT_BOOL")
			{
				return MarshalVariantBoolFromNative(variableName);
			}
			string text = _context.Global.Services.Naming.ForVariable(_typeRef);
			if (text != _marshaledTypeName)
			{
				return $"static_cast<{text}>({variableName})";
			}
			return variableName;
		}

		public override void WriteMarshalVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, ManagedMarshalValue destinationVariable, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, bool callConstructor, IRuntimeMetadataAccess metadataAccess)
		{
			writer.WriteLine(destinationVariable.Store(WriteMarshalVariableFromNative(writer, variableName, methodParameters, safeHandleShouldEmitAddRef, forNativeWrapperOfManagedMethod, metadataAccess)));
		}

		private static string MarshalVariantBoolToNative(string variableName)
		{
			return $"(({variableName}) ? IL2CPP_VARIANT_TRUE : IL2CPP_VARIANT_FALSE)";
		}

		private static string MarshalVariantBoolFromNative(string variableName)
		{
			return $"(({variableName}) != IL2CPP_VARIANT_FALSE)";
		}

		public override void WriteNativeVariableDeclarationOfType(IGeneratedMethodCodeWriter writer, string variableName)
		{
			if (_typeRef.IsPointer)
			{
				base.WriteNativeVariableDeclarationOfType(writer, variableName);
				return;
			}
			string text = "0";
			switch (_marshaledTypeName)
			{
			case "float":
				text = "0.0f";
				break;
			case "double":
				text = "0.0";
				break;
			case "IL2CPP_VARIANT_BOOL":
				text = "IL2CPP_VARIANT_FALSE";
				break;
			}
			writer.WriteLine("{0} {1} = {2};", _marshaledTypeName, variableName, text);
		}
	}
}
