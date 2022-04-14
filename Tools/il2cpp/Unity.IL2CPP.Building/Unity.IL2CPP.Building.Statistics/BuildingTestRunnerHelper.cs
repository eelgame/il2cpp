using System;
using System.IO;
using NiceIO;
using SaferMutex;
using Unity.IL2CPP.Common;

namespace Unity.IL2CPP.Building.Statistics
{
	public static class BuildingTestRunnerHelper
	{
		public const string CompilationStatsFileNamePrefix = "TestRunner_CompilationStats_";

		private static readonly object _logLock = new object();

		public static bool IsRunningUnderTestRunner => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("IL2CPP_TEST_RUNNING_UNDER_TEST_RUNNER"));

		public static NPath StatsOutputDirectory => Il2CppSpecificUtilities.GetIl2CppSolutionDirectory().ToNPath().Combine("results", "stats");

		private static string GetRunName => Environment.GetEnvironmentVariable("IL2CPP_TEST_RUN_NAME");

		public static void SaveCompilationStats(IBuildStatistics stats)
		{
			if (!IsRunningUnderTestRunner)
			{
				return;
			}
			NPath nPath = StatsOutputDirectory.Combine("TestRunner_CompilationStats_" + GetRunName + ".log");
			bool owned;
			using (global::SaferMutex.SaferMutex saferMutex = global::SaferMutex.SaferMutex.Create(initiallyOwned: true, "SaveCompilationStats", Scope.CurrentUser, out owned))
			{
				if (!owned)
				{
					saferMutex.WaitOne();
				}
				using (StreamWriter streamWriter = new StreamWriter(nPath.ToString(), append: true))
				{
					streamWriter.WriteLine("{0},{1}", stats.TotalFiles, stats.CacheHits);
				}
			}
		}

		public static IBuildStatistics LoadCompilationStats(NPath statsFile)
		{
			int num = 0;
			int num2 = 0;
			string[] array = statsFile.ReadAllLines();
			for (int i = 0; i < array.Length; i++)
			{
				string[] array2 = array[i].Split(new char[1] { ',' });
				num += int.Parse(array2[0]);
				num2 += int.Parse(array2[1]);
			}
			return new CppProgramBuildStatistics(num, num2);
		}

		public static NPath BuildAndLogStatsForTestRunner(this CppProgramBuilder builder)
		{
			IBuildStatistics statistics;
			return builder.BuildAndLogStatsForTestRunner(out statistics);
		}

		public static NPath BuildAndLogStatsForTestRunner(this CppProgramBuilder builder, out IBuildStatistics statistics)
		{
			NPath result = builder.Build(out statistics);
			SaveCompilationStats(statistics);
			return result;
		}
	}
}
