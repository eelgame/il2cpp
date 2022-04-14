using Mono.Cecil;

namespace Unity.IL2CPP.Debugger
{
	public interface ICatchPointCollectorProvider
	{
		ICatchPointCollector GetCollector(AssemblyDefinition assembly);
	}
}
