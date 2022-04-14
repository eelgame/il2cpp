using Mono.Cecil;
using Unity.IL2CPP.Contexts.Components.Base;
using Unity.IL2CPP.Metadata;

namespace Unity.IL2CPP.Contexts.Results
{
	public interface IGenericMethodCollectorResults : IMetadataIndexTableResults<Il2CppMethodSpec>, ITableResults<Il2CppMethodSpec, uint>
	{
		uint GetIndex(MethodReference method);

		bool HasIndex(MethodReference method);

		bool TryGetValue(MethodReference method, out uint genericMethodIndex);
	}
}
