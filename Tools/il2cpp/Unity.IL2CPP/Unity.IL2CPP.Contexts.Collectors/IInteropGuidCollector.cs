using Mono.Cecil;

namespace Unity.IL2CPP.Contexts.Collectors
{
	public interface IInteropGuidCollector
	{
		void Add(SourceWritingContext context, TypeReference type);
	}
}
