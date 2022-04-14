using System.Collections.Generic;
using System.Diagnostics;
using Mono.Cecil;

namespace Unity.IL2CPP.Tiny
{
	[DebuggerDisplay("{Type}|{PackedCounts}|{Size32}|{Size64}|{Offset32}|{Offset64}|{VirtualMethods.Length}")]
	public class TinyTypeEntry
	{
		public readonly TypeReference Type;

		public readonly uint PackedCounts;

		public readonly uint Size32;

		public readonly uint Size64;

		public readonly uint Offset32;

		public readonly uint Offset64;

		public readonly MethodReference[] VirtualMethods;

		public readonly IEnumerable<TypeReference> TypeHierarchy;

		public readonly IEnumerable<TypeReference> Interfaces;

		public readonly IEnumerable<int> InterfaceOffsets;

		public readonly string OffsetConstantName;

		public TinyTypeEntry(TypeReference type, uint packedCounts, uint size32, uint size64, uint offset32, uint offset64, MethodReference[] virtualMethods, IEnumerable<TypeReference> typeHierarchy, IEnumerable<TypeReference> interfaces, IEnumerable<int> interfaceOffsets, string offsetConstantName)
		{
			Type = type;
			PackedCounts = packedCounts;
			Size32 = size32;
			Size64 = size64;
			Offset32 = offset32;
			Offset64 = offset64;
			VirtualMethods = virtualMethods;
			TypeHierarchy = typeHierarchy;
			Interfaces = interfaces;
			InterfaceOffsets = interfaceOffsets;
			OffsetConstantName = offsetConstantName;
		}
	}
}
