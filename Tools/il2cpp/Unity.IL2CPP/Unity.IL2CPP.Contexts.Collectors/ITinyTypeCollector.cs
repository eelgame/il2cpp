using Mono.Cecil;

namespace Unity.IL2CPP.Contexts.Collectors
{
	public interface ITinyTypeCollector
	{
		void Add(TypeReference type);
	}
}
