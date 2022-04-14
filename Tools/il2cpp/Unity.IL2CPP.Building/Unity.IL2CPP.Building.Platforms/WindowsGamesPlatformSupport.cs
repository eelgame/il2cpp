using Unity.IL2CPP.Building.BuildDescriptions;
using Unity.IL2CPP.Building.BuildDescriptions.Mono;
using Unity.IL2CPP.Building.ToolChains;
using Unity.IL2CPP.Common;

namespace Unity.IL2CPP.Building.Platforms
{
	internal class WindowsGamesPlatformSupport : PlatformSupport
	{
		public override string BaselibPlatformName { get; } = "WindowsGames";


		public override BaselibBuildType BaselibBuildType { get; } = BaselibBuildType.DynamicLibrary;


		public override bool Supports(RuntimePlatform platform)
		{
			return platform is WindowsGamesRuntimePlatform;
		}

		public override ProgramBuildDescription PostProcessProgramBuildDescription(ProgramBuildDescription programBuildDescription)
		{
			if (!(programBuildDescription is IL2CPPOutputBuildDescription buildDescription))
			{
				return programBuildDescription;
			}
			if (programBuildDescription is UnitTestPlusPlusBuildDescription other)
			{
				return new WindowsGamesUnitTestPlusPlusBuildDescription(other);
			}
			return new WindowsIL2CPPOutputBuildDescription(buildDescription);
		}

		public override CppToolChain MakeCppToolChain(Architecture architecture, BuildConfiguration buildConfiguration, bool treatWarningsAsErrors)
		{
			return new MsvcWindowsGamesToolChain(architecture, buildConfiguration, treatWarningsAsErrors, assemblyOutput: false, useDependenciesToolChain: true, null);
		}

		public override CppToolChain MakeCppToolChain(BuildingOptions buildingOptions)
		{
			return new MsvcWindowsGamesToolChain(buildingOptions.Architecture, buildingOptions.Configuration, buildingOptions.TreatWarningsAsErrors, buildingOptions.AssemblyOutput, buildingOptions.UseDependenciesToolChain, buildingOptions.ToolChainPath, buildingOptions.DisableExceptions, buildingOptions.ShowIncludes);
		}

		public override MonoSourceFileList GetMonoSourceFileList()
		{
			return new WindowsGamesMonoSourceFileList();
		}

		public override MonoSourceFileList GetDebuggerMonoSourceFileList()
		{
			return new WindowsDebuggerMonoSourceFileList();
		}

		public override string BaselibToolchainName(Architecture architecture)
		{
			return "WindowsGames";
		}
	}
}
