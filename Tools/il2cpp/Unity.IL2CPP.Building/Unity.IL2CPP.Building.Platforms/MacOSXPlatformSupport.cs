using Unity.IL2CPP.Building.BuildDescriptions;
using Unity.IL2CPP.Building.BuildDescriptions.Mono;
using Unity.IL2CPP.Building.ToolChains;
using Unity.IL2CPP.Common;

namespace Unity.IL2CPP.Building.Platforms
{
	internal class MacOSXPlatformSupport : PlatformSupport
	{
		public override string BaselibPlatformName { get; } = "OSX";


		public override bool Supports(RuntimePlatform platform)
		{
			return platform is MacOSXRuntimePlatform;
		}

		public override CppToolChain MakeCppToolChain(Architecture architecture, BuildConfiguration buildConfiguration, bool treatWarningsAsErrors)
		{
			return new ClangToolChain(architecture, buildConfiguration, treatWarningsAsErrors, useDependenciesToolChain: false);
		}

		public override CppToolChain MakeCppToolChain(BuildingOptions buildingOptions)
		{
			return new ClangToolChain(buildingOptions.Architecture, buildingOptions.Configuration, buildingOptions.TreatWarningsAsErrors, buildingOptions.UseDependenciesToolChain, buildingOptions.DisableExceptions, buildingOptions.ShowIncludes);
		}

		public override ProgramBuildDescription PostProcessProgramBuildDescription(ProgramBuildDescription programBuildDescription)
		{
			if (!(programBuildDescription is IL2CPPOutputBuildDescription buildDescription))
			{
				return programBuildDescription;
			}
			if (programBuildDescription is UnitTestPlusPlusBuildDescription other)
			{
				return new MacOSXUnitTestPlusPlusIL2CPPOutputBuildDescription(new MacOSXUnitTestPlusPlusBuildDescription(other));
			}
			return new MacOSXIL2CPPOutputBuildDescription(buildDescription);
		}

		public override MonoSourceFileList GetMonoSourceFileList()
		{
			return new OSXMonoSourceFileList();
		}

		public override MonoSourceFileList GetDebuggerMonoSourceFileList()
		{
			return new OSXDebuggerMonoSourceFileList();
		}

		public override string BaselibToolchainName(Architecture architecture)
		{
			return "macosx64";
		}
	}
}
