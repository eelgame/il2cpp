using Mono.Cecil;

namespace Unity.IL2CPP.Contexts.Collectors
{
	public interface IWindowsRuntimeTypeWithNameCollector
	{
		void AddWindowsRuntimeTypeWithName(PrimaryCollectionContext context, TypeReference type, string typeName);
	}
}
