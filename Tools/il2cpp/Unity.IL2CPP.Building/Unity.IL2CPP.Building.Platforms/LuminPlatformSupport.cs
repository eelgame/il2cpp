using Unity.IL2CPP.Building.BuildDescriptions;
using Unity.IL2CPP.Building.ToolChains;
using Unity.IL2CPP.Common;

namespace Unity.IL2CPP.Building.Platforms
{
	public class LuminPlatformSupport : PlatformSupport
	{
		public override string BaselibPlatformName { get; } = "Lumin";


		public LuminPlatformSupport()
		{
			LuminSDK.InitializeSDK();
		}

		public override bool Supports(RuntimePlatform platform)
		{
			return platform is LuminRuntimePlatform;
		}

		public override CppToolChain MakeCppToolChain(Architecture architecture, BuildConfiguration buildConfiguration, bool treatWarningsAsErrorsInGeneratedCode)
		{
			return new LuminToolChain(architecture, buildConfiguration, treatWarningsAsErrorsInGeneratedCode, assemblyOutput: false, useDependenciesToolchain: false);
		}

		public override CppToolChain MakeCppToolChain(BuildingOptions buildingOptions)
		{
			return new LuminToolChain(buildingOptions.Architecture, buildingOptions.Configuration, buildingOptions.TreatWarningsAsErrors, buildingOptions.AssemblyOutput, buildingOptions.UseDependenciesToolChain, buildingOptions.ToolChainPath);
		}

		public override ProgramBuildDescription PostProcessProgramBuildDescription(ProgramBuildDescription programBuildDescription)
		{
			if (programBuildDescription is UnitTestPlusPlusBuildDescription)
			{
				return new LuminUnitTestPlusPlusBuildDescription(programBuildDescription as UnitTestPlusPlusBuildDescription);
			}
			if (programBuildDescription is IL2CPPOutputBuildDescription)
			{
				return new LuminCppRunnerBuildDescription(programBuildDescription as IL2CPPOutputBuildDescription);
			}
			return base.PostProcessProgramBuildDescription(programBuildDescription);
		}

		public override string BaselibToolchainName(Architecture architecture)
		{
			return "Lumin_aarch64";
		}
	}
}
