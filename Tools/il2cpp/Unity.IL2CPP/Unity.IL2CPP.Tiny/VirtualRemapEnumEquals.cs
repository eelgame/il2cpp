using System;
using Mono.Cecil;
using Unity.Cecil.Awesome;

namespace Unity.IL2CPP.Tiny
{
	internal class VirtualRemapEnumEquals : ITinyVirtualRemapHandler
	{
		private enum UnderlyingTypeSize
		{
			None,
			OneByte,
			TwoBytes,
			FourBytes,
			EightBytes
		}

		public bool ShouldRemapVirtualMethod(TinyVirtualMethodData virtualMethodData)
		{
			if (UnderlyingTypeSizeFor(virtualMethodData.DerivedDeclaringType) != 0)
			{
				return virtualMethodData.VirtualMethod.FullName == "System.Boolean System.Enum::Equals(System.Object)";
			}
			return false;
		}

		public string RemappedMethodNameFor(TinyVirtualMethodData virtualMethodData, bool returnAsByRefParameter)
		{
			UnderlyingTypeSize underlyingTypeSize = UnderlyingTypeSizeFor(virtualMethodData.DerivedDeclaringType);
			string text = (returnAsByRefParameter ? "_ret_as_byref_param" : "");
			switch (underlyingTypeSize)
			{
			case UnderlyingTypeSize.OneByte:
				return "il2cpp_virtual_remap_enum1_equals" + text;
			case UnderlyingTypeSize.TwoBytes:
				return "il2cpp_virtual_remap_enum2_equals" + text;
			case UnderlyingTypeSize.FourBytes:
				return "il2cpp_virtual_remap_enum4_equals" + text;
			case UnderlyingTypeSize.EightBytes:
				return "il2cpp_virtual_remap_enum8_equals" + text;
			default:
				throw new InvalidOperationException($"We don't know how to remap a virtual method for {virtualMethodData.VirtualMethod}");
			}
		}

		private static UnderlyingTypeSize UnderlyingTypeSizeFor(TypeReference type)
		{
			if (!type.IsEnum())
			{
				return UnderlyingTypeSize.None;
			}
			MetadataType metadataType = type.GetUnderlyingEnumType().MetadataType;
			switch (metadataType)
			{
			case MetadataType.SByte:
			case MetadataType.Byte:
				return UnderlyingTypeSize.OneByte;
			case MetadataType.Int16:
			case MetadataType.UInt16:
				return UnderlyingTypeSize.TwoBytes;
			case MetadataType.Int32:
			case MetadataType.UInt32:
				return UnderlyingTypeSize.FourBytes;
			case MetadataType.Int64:
			case MetadataType.UInt64:
				return UnderlyingTypeSize.EightBytes;
			default:
				throw new InvalidOperationException($"Invalid enum metadata type: {metadataType}");
			}
		}
	}
}
