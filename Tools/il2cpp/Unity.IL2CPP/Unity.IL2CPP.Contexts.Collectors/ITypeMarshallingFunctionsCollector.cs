using Mono.Cecil;

namespace Unity.IL2CPP.Contexts.Collectors
{
	public interface ITypeMarshallingFunctionsCollector
	{
		void Add(SourceWritingContext context, TypeDefinition type);
	}
}
