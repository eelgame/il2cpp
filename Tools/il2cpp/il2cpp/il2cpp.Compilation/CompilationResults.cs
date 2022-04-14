using Unity.IL2CPP.Building.Statistics;

namespace il2cpp.Compilation
{
	public class CompilationResults
	{
		public readonly IBuildStatistics Statistics;

		public CompilationResults(IBuildStatistics statistics)
		{
			Statistics = statistics;
		}
	}
}
