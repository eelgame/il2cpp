using System;
using NiceIO;
using Unity.IL2CPP.Common;
using Unity.Options;

namespace il2cpp
{
	[ProgramOptions]
	public class IL2CPPOptions
	{
		[HelpDetails("One or more paths to assemblies to convert", "path")]
		public static NPath[] Assembly;

		[HelpDetails("One or more directories containing assemblies to convert", "path")]
		public static NPath[] Directory;

		[HelpDetails("The directory where generated C++ code is written", "path")]
		public static NPath Generatedcppdir;

		[HelpDetails("The directory where non-source code data will be written", "path")]
		public static NPath DataFolder;

		[HelpDetails("The directory where symbol information will be written", "path")]
		public static NPath SymbolsFolder;

		[HelpDetails("Path to an il2cpp plugin assembly", null)]
		public static NPath[] Plugin;

		[HelpDetails("Path to MapFileParser binary", null)]
		public static NPath MapFileParser;

		[HelpDetails("Enable generation of a profiler report", null)]
		public static bool ProfilerReport;

		[HelpDetails("Convert the provided assemblies to C++", null)]
		public static bool ConvertToCpp;

		[HelpDetails("Compile generated C++ code", null)]
		public static bool CompileCpp;

		[HelpDetails("One or more files containing a list of additonal generic instance types that should be included in the generated code", "path")]
		public static NPath[] ExtraTypesFile;

		[HideFromHelp]
		public static NPath ExecutableAssembliesFolderOnDevice;

		[HideFromHelp]
		public static NPath[] SearchDir;

		[HideFromHelp]
		public static string EntryAssemblyName;

		[HideFromHelp]
		public static bool DevelopmentMode;

		[OptionAlias("j")]
		[HelpDetails("The number of cores to use during code conversion.  Defaults to processor count", null)]
		public static int Jobs;

		[HelpDetails("The name of an assembly (including .dll) to emit debug information for.  If this is provided, debug information from all others will be ignored.", null)]
		public static string[] DebugAssemblyName;

		[HideFromHelp]
		public static bool DebugEnableAttach;

		[HideFromHelp]
		public static NPath DebugRiderInstallPath;

		[HideFromHelp]
		public static NPath DebugSolutionPath;

		public static void SetToDefaults()
		{
			Generatedcppdir = null;
			DataFolder = null;
			Assembly = new NPath[0];
			ConvertToCpp = false;
			CompileCpp = false;
			ExtraTypesFile = null;
			Directory = null;
			Plugin = null;
			SearchDir = new NPath[0];
			EntryAssemblyName = null;
			DevelopmentMode = false;
			Jobs = Environment.ProcessorCount;
			DebugAssemblyName = new string[0];
			DebugEnableAttach = false;
			DebugRiderInstallPath = null;
			DebugSolutionPath = null;
		}

		public static string[] InitAndPrepare(string[] commandLine, Type[] types)
		{
			SetToDefaults();
			return OptionsParser.Prepare(commandLine, types, OptionsHelpers.CommonCustomOptionParser);
		}
	}
}
