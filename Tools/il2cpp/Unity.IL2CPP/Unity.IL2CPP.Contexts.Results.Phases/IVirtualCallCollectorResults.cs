using Unity.IL2CPP.Contexts.Components.Base;
using Unity.IL2CPP.Metadata.RuntimeTypes;

namespace Unity.IL2CPP.Contexts.Results.Phases
{
	public interface IVirtualCallCollectorResults : IMetadataIndexTableResults<IIl2CppRuntimeType[]>, ITableResults<IIl2CppRuntimeType[], uint>
	{
	}
}
