using Mono.Cecil;
using NiceIO;

namespace Unity.IL2CPP.Contexts.Services
{
	public interface IPathFactoryService
	{
		string GetFileName(string fileName);

		string GetFileNameForAssembly(AssemblyDefinition assembly, string fileName);

		NPath GetFilePath(NPath filePath);
	}
}
