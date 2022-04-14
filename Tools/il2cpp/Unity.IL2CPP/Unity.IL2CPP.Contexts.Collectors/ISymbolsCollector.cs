using NiceIO;

namespace Unity.IL2CPP.Contexts.Collectors
{
	public interface ISymbolsCollector
	{
		void CollectLineNumberInformation(ReadOnlyContext context, NPath CppSourcePath);
	}
}
