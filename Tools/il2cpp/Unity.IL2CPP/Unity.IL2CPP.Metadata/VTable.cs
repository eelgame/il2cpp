using System.Collections.Generic;
using System.Collections.ObjectModel;
using Mono.Cecil;

namespace Unity.IL2CPP.Metadata
{
	public class VTable
	{
		private readonly ReadOnlyCollection<MethodReference> _slots;

		private readonly Dictionary<TypeReference, int> _interfaceOffsets;

		public ReadOnlyCollection<MethodReference> Slots => _slots;

		public Dictionary<TypeReference, int> InterfaceOffsets => _interfaceOffsets;

		public VTable(ReadOnlyCollection<MethodReference> slots, Dictionary<TypeReference, int> interfaceOffsets)
		{
			_slots = slots;
			_interfaceOffsets = interfaceOffsets;
		}
	}
}
