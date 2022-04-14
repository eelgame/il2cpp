using System.Collections.Generic;
using System.Linq;
using NiceIO;
using Unity.IL2CPP.Building.BuildDescriptions;
using Unity.IL2CPP.Building.Hashing;
using Unity.IL2CPP.Building.Statistics;
using Unity.IL2CPP.Building.ToolChains;
using Unity.IL2CPP.Common;

namespace Unity.IL2CPP.Building.Platforms
{
	public class WindowsIL2CPPOutputBuildDescription : IL2CPPOutputBuildDescription
	{
		public override NPath PchCSourceFile => base.PchDirectory.Combine("pch-c.c");

		public override NPath PchCppSourceFile => base.PchDirectory.Combine("pch-cpp.cpp");

		public WindowsIL2CPPOutputBuildDescription(IL2CPPOutputBuildDescription buildDescription)
			: base(buildDescription)
		{
		}

		public override IEnumerable<NPath> SourceFilesIn(params NPath[] foldersToGlob)
		{
			foreach (NPath item in base.SourceFilesIn(foldersToGlob))
			{
				yield return item;
			}
			if (!(_architecture is x64Architecture))
			{
				yield break;
			}
			foreach (NPath item2 in from f in foldersToGlob.SelectMany((NPath d) => d.Files("*.asm*", recurse: true))
				where f.HasExtension("asm")
				select f)
			{
				yield return item2;
			}
		}

		public override IEnumerable<string> AdditionalDefinesFor(NPath sourceFile)
		{
			foreach (string item in base.AdditionalDefinesFor(sourceFile))
			{
				yield return item;
			}
			if (!OutputFile.HasExtension(".dll"))
			{
				yield return "LIBIL2CPP_IS_IN_EXECUTABLE=1";
			}
		}

		public override void OnBeforeLink(HeaderFileHashProvider headerHashProvider, NPath workingDirectory, IEnumerable<NPath> objectFiles, CppToolChainContext toolChainContext, bool forceRebuild, bool verbose, bool includeFileNamesInHashes)
		{
			base.OnBeforeLink(headerHashProvider, workingDirectory, objectFiles, toolChainContext, forceRebuild, verbose, includeFileNamesInHashes);
			NPath outputFile = OutputFile;
			if (!outputFile.HasExtension(".dll") && _runtimeLibrary != RuntimeBuildType.Tiny)
			{
				NPath outputFile2 = outputFile.Parent.Combine("Libil2cppLackey.dll");
				NPath cacheDirectory = ((GlobalCacheDirectory != null) ? GlobalCacheDirectory.Combine("Libil2cppLackey") : null);
				string[] additionalLinkerFlags = new string[2] { "/ENTRY:DllMain", "/NODEFAULTLIB" };
				SimpleDirectoryProgramBuildDescription simpleDirectoryProgramBuildDescription = new SimpleDirectoryProgramBuildDescription(CommonPaths.Il2CppRoot.Combine("Libil2cppLackey"), outputFile2, cacheDirectory, null, additionalLinkerFlags);
				MsvcToolChainContext msvcToolChainContext = (MsvcToolChainContext)toolChainContext;
				BuildingOptions buildingOptions = new BuildingOptions
				{
					Architecture = _architecture,
					Configuration = BuildConfiguration.Release,
					TreatWarningsAsErrors = msvcToolChainContext.TreatWarningsAsErrors,
					UseDependenciesToolChain = msvcToolChainContext.UseDependenciesToolChain,
					DisableExceptions = true
				};
				MsvcToolChain cppToolChain = (MsvcToolChain)_platform.MakeCppToolChain(buildingOptions);
				CppProgramCMakeGenerator.AddBuild(simpleDirectoryProgramBuildDescription, cppToolChain);
				new CppProgramBuilder(cppToolChain, simpleDirectoryProgramBuildDescription, headerHashProvider, verbose: true, forceRebuild, includeFileNamesInHashes: false).BuildAndLogStatsForTestRunner();
			}
		}

		public override IEnumerable<NPath> GetDynamicLibraries(BuildConfiguration configuration)
		{
			foreach (NPath dynamicLibrary in base.GetDynamicLibraries(configuration))
			{
				yield return dynamicLibrary;
			}
			NPath outputFile = OutputFile;
			if (!outputFile.HasExtension(".dll") && _runtimeLibrary != RuntimeBuildType.Tiny)
			{
				yield return outputFile.Parent.Combine("Libil2cppLackey.lib");
			}
		}
	}
}
