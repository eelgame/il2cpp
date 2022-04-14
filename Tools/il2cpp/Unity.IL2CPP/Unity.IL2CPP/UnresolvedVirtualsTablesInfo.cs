using System.Collections.ObjectModel;
using Unity.IL2CPP.Metadata;
using Unity.IL2CPP.Metadata.RuntimeTypes;

namespace Unity.IL2CPP
{
	public struct UnresolvedVirtualsTablesInfo
	{
		public TableInfo MethodPointersInfo;

		public ReadOnlyCollection<IIl2CppRuntimeType[]> SignatureTypes;
	}
}
