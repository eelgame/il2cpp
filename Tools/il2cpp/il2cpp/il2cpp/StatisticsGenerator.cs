using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NiceIO;
using Unity.IL2CPP.Building.Statistics;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts.Results;
using Unity.IL2CPP.Statistics;
using Unity.MiniProfiling;

namespace il2cpp
{
	public static class StatisticsGenerator
	{
		private class TimedSectionSummaryComparer : IComparer<MiniProfiler.TimedSection>
		{
			public int Compare(MiniProfiler.TimedSection x, MiniProfiler.TimedSection y)
			{
				return x.Summary.CompareTo(y.Summary);
			}
		}

		private const double BytesInKilobyte = 1024.0;

		private const string StatisticsLogFileName = "statistics.txt";

		public static NPath DetermineAndSetupOutputDirectory()
		{
			NPath nPath = StatisticsOptions.StatsOutputDir;
			if (!nPath.Exists())
			{
				nPath.CreateDirectory();
			}
			if (StatisticsOptions.EnableUniqueStatsOutputDir)
			{
				string text = Path.GetRandomFileName();
				string statsRunName = StatisticsOptions.StatsRunName;
				if (!string.IsNullOrEmpty(statsRunName))
				{
					text = statsRunName + "_-_" + text;
				}
				nPath = nPath.Combine(text);
				nPath.CreateDirectory();
			}
			return nPath;
		}

		public static void Generate(NPath statsOutputDirectory, NPath generatedCppDirectory, IStatsResults conversionStats, IBuildStatistics buildStats, ProfilerSnapshot profilerSnapshot, IEnumerable<string> commandLineArguments, IEnumerable<NPath> assembliesConverted)
		{
			WriteSingleLog(statsOutputDirectory, generatedCppDirectory, conversionStats, buildStats, profilerSnapshot, commandLineArguments, assembliesConverted);
			WriteBasicDataToStdout(statsOutputDirectory, profilerSnapshot);
			if (StatisticsOptions.IncludeGenFilesWithStats)
			{
				CopyGeneratedFilesIntoStatsOutput(statsOutputDirectory, generatedCppDirectory);
			}
		}

		private static void WriteSingleLog(NPath statsOutputDirectory, NPath generatedCppDirectory, IStatsResults conversionStats, IBuildStatistics buildStats, ProfilerSnapshot profilerSnapshot, IEnumerable<string> commandLineArguments, IEnumerable<NPath> assembliesConverted)
		{
			using (StreamWriter writer = new StreamWriter(statsOutputDirectory.Combine("statistics.txt").ToString()))
			{
				WriteGeneralLog(writer, commandLineArguments, assembliesConverted);
				WriteStatsLog(writer, conversionStats, buildStats);
				WriteProfilerLog(writer, profilerSnapshot);
				WriteGeneratedFileLog(writer, generatedCppDirectory);
			}
		}

		private static void WriteGeneralLog(TextWriter writer, IEnumerable<string> commandLineArguments, IEnumerable<NPath> assembliesConverted)
		{
			writer.WriteLine("----IL2CPP Arguments----");
			writer.WriteLine(commandLineArguments.SeparateWithSpaces());
			writer.WriteLine();
			if (assembliesConverted == null)
			{
				return;
			}
			writer.WriteLine("----Assemblies Converted-----");
			foreach (NPath item in assembliesConverted)
			{
				writer.WriteLine("\t{0}", item);
			}
			writer.WriteLine();
		}

		private static void WriteGeneratedFileLog(TextWriter writer, NPath generatedOutputDirectory)
		{
			List<FileInfo> list = CollectGeneratedFileInfo(generatedOutputDirectory);
			writer.WriteLine("----Generated Files----");
			foreach (FileInfo item in list)
			{
				writer.WriteLine(item.Name + "\t" + item.Length);
			}
			writer.WriteLine();
		}

		public static void WriteStatsLog(TextWriter writer, IStatsResults conversionStats, IBuildStatistics buildStats)
		{
			writer.WriteLine("----- il2cpp Statistics -----");
			if (conversionStats != null)
			{
				writer.WriteLine("General:");
				writer.WriteLine("\tConversion Time: {0} s", (double)conversionStats.ConversionMilliseconds / 1000.0);
				writer.WriteLine($"\tJob Count: {conversionStats.JobCount}");
				writer.WriteLine($"\tProcessor Count: {Environment.ProcessorCount}");
				writer.WriteLine("\tFiles Written: {0}", conversionStats.FilesWritten);
				writer.WriteLine("\tString Literals: {0}", conversionStats.StringLiterals);
				writer.WriteLine("Methods:");
				writer.WriteLine("\tTotal Methods: {0}", conversionStats.Methods);
				writer.WriteLine("\tNon-Generic Methods: {0}", conversionStats.Methods - (conversionStats.GenericTypeMethods + conversionStats.GenericMethods));
				writer.WriteLine("\tGeneric Type Methods: {0}", conversionStats.GenericTypeMethods);
				writer.WriteLine("\tGeneric Methods: {0}", conversionStats.GenericMethods);
				writer.WriteLine("\tShared Methods: {0}", conversionStats.ShareableMethods);
				writer.WriteLine("\tMethods with Tail Calls : {0}", conversionStats.TailCallsEncountered);
				writer.WriteLine("Metadata:");
				writer.WriteLine("\tTotal: {0:N2} kb", (double)conversionStats.MetadataTotal / 1024.0);
				foreach (KeyValuePair<string, long> metadataStream in conversionStats.MetadataStreams)
				{
					writer.WriteLine("\t{0}: {1:N2} kb", metadataStream.Key, (double)metadataStream.Value / 1024.0);
				}
				writer.WriteLine("Codegen:");
				writer.WriteLine("\tNullChecks : {0}", conversionStats.TotalNullChecks);
				writer.WriteLine("Interop:");
				writer.WriteLine($"\tWindows Runtime boxed types : {conversionStats.WindowsRuntimeBoxedTypes}");
				writer.WriteLine($"\tWindows Runtime types with names : {conversionStats.WindowsRuntimeTypesWithNames}");
				writer.WriteLine($"\tNative to managed interface adapters : {conversionStats.NativeToManagedInterfaceAdapters}");
				writer.WriteLine($"\tArray COM callable wrappers : {conversionStats.ArrayComCallableWrappers}");
				writer.WriteLine($"\tCOM callable wrappers : {conversionStats.ComCallableWrappers}");
				writer.WriteLine($"\tCOM callable wrapper methods that were implemented : {conversionStats.ImplementedComCallableWrapperMethods}");
				writer.WriteLine($"\tCOM callable wrapper methods that were stripped : {conversionStats.StrippedComCallableWrapperMethods}");
				writer.WriteLine($"\tCOM callable wrapper methods that were forwarded to call base class method : {conversionStats.ForwardedToBaseClassComCallableWrapperMethods}");
			}
			if (buildStats != null && buildStats.TotalFiles > 0)
			{
				writer.WriteLine("Compilation:");
				writer.WriteLine("\tTotal Files: {0}", buildStats.TotalFiles);
				writer.WriteLine("\tFiles Compiled: {0}", buildStats.FilesCompiled);
				writer.WriteLine("\tCache Hits: {0}", buildStats.CacheHits);
				writer.WriteLine("\tCache Hit %: {0}", (int)((double)buildStats.CacheHits / (double)buildStats.TotalFiles * 100.0));
			}
			writer.WriteLine();
			writer.WriteLine();
		}

		private static void WriteProfilerLog(TextWriter writer, ProfilerSnapshot profilerSnapshot)
		{
			WriteAssemblyConversionTimes(writer, profilerSnapshot);
			WriteMiscTimes(writer, profilerSnapshot);
		}

		private static void WriteAssemblyConversionTimes(TextWriter writer, ProfilerSnapshot profilerSnapshot)
		{
			writer.WriteLine("----Assembly Conversion Times----");
			List<MiniProfiler.TimedSection> list = profilerSnapshot.GetSectionsByLabel("Convert").ToList();
			list.Sort(new TimedSectionSummaryComparer());
			foreach (MiniProfiler.TimedSection item in list)
			{
				writer.WriteLine("\t{0}", item.Summary);
			}
			writer.WriteLine();
		}

		private static void WriteMiscTimes(TextWriter writer, ProfilerSnapshot profilerSnapshot)
		{
			writer.WriteLine("----Misc Timing----");
			writer.WriteLine("\tPreProcessStage: {0}", SectionDurationFor("PreProcessStage", profilerSnapshot));
			writer.WriteLine("\tGenericsCollector.Collect: {0}", SectionDurationFor("Collect", profilerSnapshot));
			writer.WriteLine("\tWriteGenerics: {0}", SectionDurationFor("WriteGenerics", profilerSnapshot));
			writer.WriteLine("\tAllAssemblyConversion: {0}", SectionDurationFor("AllAssemblyConversion", profilerSnapshot));
			if (CodeGenOptions.EmitSourceMapping)
			{
				writer.WriteLine("\tSymbolsCollection: {0}", SectionDurationFor("SymbolsCollection", profilerSnapshot));
			}
			writer.WriteLine();
		}

		private static string SectionDurationFor(string level, ProfilerSnapshot profilerSnapshot)
		{
			if (profilerSnapshot.GetSectionsByLabel(level).Any())
			{
				return profilerSnapshot.GetSectionsByLabel(level).First().Duration.ToString();
			}
			return "None";
		}

		private static void WriteBasicDataToStdout(NPath statsOutputDirectory, ProfilerSnapshot profilerSnapshot)
		{
			WriteAssemblyConversionTimes(ConsoleOutput.Info.Stdout, profilerSnapshot);
			WriteMiscTimes(ConsoleOutput.Info.Stdout, profilerSnapshot);
			ConsoleOutput.Info.WriteLine("Statistics written to : {0}", statsOutputDirectory);
		}

		private static void CopyGeneratedFilesIntoStatsOutput(NPath statsOutputDirectory, NPath generatedCppDirectory)
		{
			NPath nPath = statsOutputDirectory.Combine("generated");
			if (nPath.Exists())
			{
				nPath.Delete();
			}
			if (!nPath.Exists())
			{
				nPath.CreateDirectory();
			}
			generatedCppDirectory.CopyFiles(nPath, recurse: true);
		}

		private static List<FileInfo> CollectGeneratedFileInfo(NPath outputDirectory)
		{
			if (!outputDirectory.Exists())
			{
				return new List<FileInfo>();
			}
			List<string> list = (from f in outputDirectory.Files().Where(IsGeneratedFile)
				select f.ToString()).ToList();
			list.Sort();
			return list.Select((string f) => new FileInfo(f.ToString())).ToList();
		}

		private static bool IsGeneratedFile(NPath path)
		{
			return path.HasExtension("cpp", "h");
		}
	}
}
