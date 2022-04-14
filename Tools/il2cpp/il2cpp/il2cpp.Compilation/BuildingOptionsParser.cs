using System;
using System.Collections.Generic;
using System.Linq;
using NiceIO;
using Unity.IL2CPP.Building;
using Unity.IL2CPP.Building.BuildDescriptions;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Common.Profiles;
using Unity.Options;

namespace il2cpp.Compilation
{
	public class BuildingOptionsParser
	{
		[ProgramOptions]
		public class BuildingOptionsArgs
		{
			[HideFromHelp]
			public static RuntimePlatform Platform;

			[HideFromHelp]
			public static Architecture Architecture;

			[HideFromHelp]
			public static bool UseSGEN;

			[HelpDetails("The build configuration.  Debug|Release", null)]
			public static BuildConfiguration Configuration;

			[HideFromHelp]
			public static NPath ToolChainPath;

			[HelpDetails("Path to output the compiled binary", null)]
			public static NPath Outputpath;

			[HelpDetails("Optional. Specifies path of IL2CPP data directory relative to deployed application working directory.", null)]
			public static string RelativeDataPath;

			[HelpDetails("Defines for generated C++ code compilation", null)]
			public static string[] AdditionalDefines;

			[HelpDetails("One or more additional libraries to link to generated code", null)]
			public static string[] AdditionalLibraries;

			[HelpDetails("One or more additional include directories", "path")]
			public static NPath[] AdditionalIncludeDirectories;

			[HelpDetails("One or more additional link directories", "path")]
			public static NPath[] AdditionalLinkDirectories;

			[HelpDetails("Additional C++ files to include", "path")]
			public static NPath[] AdditionalCpp;

			[HelpDetails("Enables verbose output from tools involved in building", null)]
			public static bool Verbose;

			[HelpDetails("A directory to use for caching compilation related files", "path")]
			public static NPath Cachedirectory;

			[HelpDetails("Forces a rebuild", null)]
			public static bool Forcerebuild;

			[HelpDetails("Additional flags to pass to the C++ compiler", null)]
			public static string CompilerFlags;

			[HelpDetails("Additional flags to pass to the linker", null)]
			public static string LinkerFlags;

			[HelpDetails("Links il2cpp as static library to the executable", null)]
			public static bool Libil2cppStatic;

			[HelpDetails("Disable lumping for the runtime library", null)]
			public static bool DisableRuntimeLumping;

			[HelpDetails("Cache directory to use when building libil2cpp as dynamic link library", null)]
			public static NPath Libil2cppCacheDirectory;

			[HelpDetails("Enables warnings as errors for compiling generated C++ code", null)]
			public static bool TreatWarningsAsErrors;

			[HideFromHelp]
			public static bool IncludeFileNamesInHashes;

			[HelpDetails("Enables assembly code output from the C++ compiler", null)]
			public static bool AssemblyOutput;

			[HideFromHelp]
			public static bool UseDependenciesToolChain;

			[HideFromHelp]
			public static bool DontLinkCrt;

			[HideFromHelp]
			public static bool SetEnvironmentVariables;

			[HideFromHelp]
			public static NPath BaselibDirectory;

			[HideFromHelp]
			public static bool AvoidDynamicLibraryCopy;

			[HideFromHelp]
			public static NPath SysrootPath;

			[HideFromHelp]
			public static string GenerateCmake;

			[HideFromHelp]
			public static BuildShell.CommandLogMode CommandLog;

			public static void SetToDefaults()
			{
				Platform = null;
				Architecture = null;
				Configuration = BuildConfiguration.Release;
				ToolChainPath = null;
				Outputpath = null;
				AdditionalDefines = new string[0];
				AdditionalLibraries = new string[0];
				AdditionalIncludeDirectories = new NPath[0];
				AdditionalLinkDirectories = new NPath[0];
				AdditionalCpp = new NPath[0];
				Verbose = false;
				Cachedirectory = null;
				Forcerebuild = false;
				CompilerFlags = null;
				LinkerFlags = null;
				Libil2cppStatic = false;
				DisableRuntimeLumping = false;
				Libil2cppCacheDirectory = null;
				TreatWarningsAsErrors = false;
				IncludeFileNamesInHashes = false;
				UseSGEN = false;
				UseDependenciesToolChain = false;
				AssemblyOutput = false;
				DontLinkCrt = false;
				SetEnvironmentVariables = false;
				BaselibDirectory = null;
				AvoidDynamicLibraryCopy = false;
				SysrootPath = null;
				GenerateCmake = null;
				CommandLog = BuildShell.CommandLogMode.Off;
			}
		}

		internal static bool GenerateCMakeEnabled => !string.IsNullOrEmpty(BuildingOptionsArgs.GenerateCmake);

		public static void SetToDefaults()
		{
			BuildingOptionsArgs.SetToDefaults();
			IL2CPPOptions.Generatedcppdir = null;
		}

		public static void Parse(string[] args, NPath[] inputAssemblies, NPath generatedCppDir, out RuntimePlatform platform, out BuildingOptions buildingOptions, bool developmentMode)
		{
			Parse(args, inputAssemblies, generatedCppDir, Profile.UnityAot, RuntimeBackend.Big, out platform, out buildingOptions, developmentMode, enableDebugger: false, debuggerOff: false);
		}

		public static void Parse(string[] args, NPath[] inputAssemblies, NPath generatedCppDir, RuntimeProfile profile, RuntimeBackend runtimeBackend, out RuntimePlatform platform, out BuildingOptions buildingOptions, bool developmentMode, bool enableDebugger, bool debuggerOff)
		{
			SetToDefaults();
			OptionsParser.Prepare(args, typeof(BuildingOptionsParser).Assembly, includeReferencedAssemblies: true, OptionsHelpers.CommonCustomOptionParser);
			Parse(generatedCppDir, inputAssemblies, profile, runtimeBackend, out platform, out buildingOptions, developmentMode, enableDebugger, debuggerOff);
		}

		public static void Parse(NPath generatedCppDir, NPath[] inputAssemblies, RuntimeProfile profile, RuntimeBackend runtimeBackend, out RuntimePlatform platform, out BuildingOptions buildingOptions, bool developmentMode, bool enableDebugger, bool debuggerOff)
		{
			platform = ((BuildingOptionsArgs.Platform != null) ? BuildingOptionsArgs.Platform : RuntimePlatform.Current);
			Architecture architecture = ((BuildingOptionsArgs.Architecture != null) ? BuildingOptionsArgs.Architecture : Architecture.BestThisMachineCanRun);
			NPath nPath = null;
			if (BuildingOptionsArgs.Outputpath != null)
			{
				nPath = BuildingOptionsArgs.Outputpath;
			}
			else
			{
				NPath nPath2 = inputAssemblies[0];
				nPath = nPath2.Parent.Combine("il2cpp_" + nPath2.FileName);
			}
			NPath toolChainPath = null;
			if (BuildingOptionsArgs.ToolChainPath != null)
			{
				toolChainPath = BuildingOptionsArgs.ToolChainPath;
			}
			NPath sysrootPath = null;
			if (BuildingOptionsArgs.SysrootPath != null)
			{
				sysrootPath = BuildingOptionsArgs.SysrootPath;
			}
			buildingOptions = new BuildingOptions
			{
				Architecture = architecture,
				Configuration = BuildingOptionsArgs.Configuration,
				ToolChainPath = toolChainPath,
				OutputPath = nPath.MakeAbsolute(),
				SourceDirectory = generatedCppDir.MakeAbsolute(),
				RelativeDataPath = BuildingOptionsArgs.RelativeDataPath,
				AdditionalCpp = BuildingOptionsArgs.AdditionalCpp,
				AdditionalDefines = BuildingOptionsArgs.AdditionalDefines,
				AdditionalIncludeDirectories = BuildingOptionsArgs.AdditionalIncludeDirectories.Select((NPath d) => d.MakeAbsolute()),
				AdditionalLibraries = BuildingOptionsArgs.AdditionalLibraries,
				AdditionalLinkDirectories = BuildingOptionsArgs.AdditionalLinkDirectories.Select((NPath d) => d.MakeAbsolute()),
				CacheDirectory = ((BuildingOptionsArgs.Cachedirectory == null) ? null : BuildingOptionsArgs.Cachedirectory.MakeAbsolute()),
				Verbose = BuildingOptionsArgs.Verbose,
				ForceRebuild = BuildingOptionsArgs.Forcerebuild,
				CompilerFlags = BuildingOptionsArgs.CompilerFlags,
				LinkerFlags = BuildingOptionsArgs.LinkerFlags,
				LibIL2CPPCacheDirectory = ((BuildingOptionsArgs.Libil2cppCacheDirectory != null) ? BuildingOptionsArgs.Libil2cppCacheDirectory.MakeAbsolute() : null),
				TreatWarningsAsErrors = BuildingOptionsArgs.TreatWarningsAsErrors,
				UseDependenciesToolChain = BuildingOptionsArgs.UseDependenciesToolChain,
				AssemblyOutput = BuildingOptionsArgs.AssemblyOutput,
				IncludeFileNamesInHashes = BuildingOptionsArgs.IncludeFileNamesInHashes,
				DisableRuntimeLumping = BuildingOptionsArgs.DisableRuntimeLumping,
				DontLinkCrt = BuildingOptionsArgs.DontLinkCrt,
				SetEnvironmentVariables = BuildingOptionsArgs.SetEnvironmentVariables,
				BaselibDirectory = BuildingOptionsArgs.BaselibDirectory,
				AvoidDynamicLibraryCopy = BuildingOptionsArgs.AvoidDynamicLibraryCopy,
				SysrootPath = sysrootPath,
				GenerateCmake = BuildingOptionsArgs.GenerateCmake,
				CommandLog = BuildingOptionsArgs.CommandLog
			};
			if (developmentMode)
			{
				List<string> list = buildingOptions.AdditionalDefines.ToList();
				list.Add("IL2CPP_DEVELOPMENT=1");
				buildingOptions.AdditionalDefines = list.ToArray();
			}
			buildingOptions.RuntimeGC = (BuildingOptionsArgs.UseSGEN ? RuntimeGC.SGEN : RuntimeGC.BDWGC);
			if (BuildingOptionsArgs.UseSGEN)
			{
				throw new ArgumentException("SGEN is only available with the mono runtime");
			}
			if (runtimeBackend == RuntimeBackend.Tiny)
			{
				buildingOptions.Runtime = RuntimeBuildType.Tiny;
			}
			else if (BuildingOptionsArgs.Libil2cppStatic)
			{
				buildingOptions.Runtime = RuntimeBuildType.LibIL2CPPStatic;
			}
			else
			{
				buildingOptions.Runtime = RuntimeBuildType.LibIL2CPPDynamic;
			}
			if (profile == Profile.UnityTiny && DebuggerBuildUtils.DetermineBuildOptions(enableDebugger, debuggerOff) != DebuggerBuildOptions.DebuggerEnabled)
			{
				buildingOptions.DisableExceptions = true;
			}
		}
	}
}
