using NiceIO;

namespace Unity.IL2CPP.Contexts.Collectors
{
	public interface IMatchedAssemblyMethodSourceFilesCollector
	{
		void Add(NPath fileName);
	}
}
