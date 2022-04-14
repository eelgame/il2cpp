using System.IO;

namespace Unity.IL2CPP.Contexts.Results
{
	public interface ISymbolsCollectorResults
	{
		void SerializeToJson(StreamWriter outputStream);
	}
}
