using Mono.Cecil;
using Unity.IL2CPP.Contexts.Components.Base;

namespace Unity.IL2CPP.Contexts.Results.Phases
{
	public interface IMethodCollectorResults : IMetadataIndexTableResults<MethodReference>, ITableResults<MethodReference, uint>
	{
	}
}
