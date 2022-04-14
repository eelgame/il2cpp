using Mono.Cecil;
using Unity.IL2CPP.Contexts.Components.Base;

namespace Unity.IL2CPP.Contexts.Results.Phases
{
	public interface IReversePInvokeWrapperCollectorResults : IMetadataIndexTableResults<MethodReference>, ITableResults<MethodReference, uint>
	{
	}
}
