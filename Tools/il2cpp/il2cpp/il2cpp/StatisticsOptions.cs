using NiceIO;
using Unity.Options;

namespace il2cpp
{
	[ProgramOptions]
	public sealed class StatisticsOptions
	{
		[HelpDetails("The directory where statistics information will be written", "path")]
		public static NPath StatsOutputDir;

		[HideFromHelp]
		public static bool EnableUniqueStatsOutputDir = false;

		[HideFromHelp]
		public static bool IncludeGenFilesWithStats = false;

		[HideFromHelp]
		public static string StatsRunName = string.Empty;

		public static void SetToDefaults()
		{
			StatsOutputDir = null;
			EnableUniqueStatsOutputDir = false;
			IncludeGenFilesWithStats = false;
			StatsRunName = string.Empty;
		}
	}
}
