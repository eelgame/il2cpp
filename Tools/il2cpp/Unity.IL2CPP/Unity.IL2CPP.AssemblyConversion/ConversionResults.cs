using System.Collections.ObjectModel;
using NiceIO;
using Unity.IL2CPP.Contexts.Results;

namespace Unity.IL2CPP.AssemblyConversion
{
	public class ConversionResults
	{
		public readonly ReadOnlyCollection<NPath> ConvertedAssemblies;

		public readonly IStatsResults Stats;

		public readonly ReadOnlyCollection<NPath> MatchedAssemblyMethodSourceFiles;

		public readonly ReadOnlyCollection<string> LoggedMessages;

		public ConversionResults(ReadOnlyCollection<NPath> convertedAssemblies, IStatsResults statsResults, ReadOnlyCollection<NPath> matchedAssemblyMethodSourceFiles, ReadOnlyCollection<string> loggedMessages)
		{
			ConvertedAssemblies = convertedAssemblies;
			Stats = statsResults;
			MatchedAssemblyMethodSourceFiles = matchedAssemblyMethodSourceFiles;
			LoggedMessages = loggedMessages;
		}
	}
}
