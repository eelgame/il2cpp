using Mono.Cecil;

namespace Unity.IL2CPP.Contexts.Collectors
{
	public interface ICppIncludeDepthCalculatorCache
	{
		int GetOrCalculateDepth(TypeReference type);
	}
}
