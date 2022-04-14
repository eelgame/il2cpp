using Unity.IL2CPP.Building.BuildDescriptions;
using Unity.IL2CPP.Building.BuildDescriptions.Mono;
using Unity.IL2CPP.Building.ToolChains;
using Unity.IL2CPP.Common;

namespace Unity.IL2CPP.Building.Platforms
{
	internal class WindowsDesktopPlatformSupport : PlatformSupport
	{
		public override string BaselibPlatformName { get; } = "Windows";


		public override BaselibBuildType BaselibBuildType { get; } = BaselibBuildType.DynamicLibrary;


		public override bool Supports(RuntimePlatform platform)
		{
			return platform is WindowsDesktopRuntimePlatform;
		}

		public override ProgramBuildDescription PostProcessProgramBuildDescription(ProgramBuildDescription programBuildDescription)
		{
			if (!(programBuildDescription is IL2CPPOutputBuildDescription buildDescription))
			{
				return programBuildDescription;
			}
			if (programBuildDescription is UnitTestPlusPlusBuildDescription other)
			{
				return new WindowsDesktopUnitTestPlusPlusBuildDescription(other);
			}
			return new WindowsIL2CPPOutputBuildDescription(buildDescription);
		}

		public override CppToolChain MakeCppToolChain(Architecture architecture, BuildConfiguration buildConfiguration, bool treatWarningsAsErrors)
		{
			return new MsvcDesktopToolChain(architecture, buildConfiguration, treatWarningsAsErrors, assemblyOutput: false, useDependenciesToolChain: false);
		}

		public override CppToolChain MakeCppToolChain(Architecture architecture, BuildConfiguration buildConfiguration, bool treatWarningsAsErrors, bool assemblyOutput)
		{
			return new MsvcDesktopToolChain(architecture, buildConfiguration, treatWarningsAsErrors, assemblyOutput, useDependenciesToolChain: false);
		}

		public override CppToolChain MakeCppToolChain(BuildingOptions buildingOptions)
		{
			return new MsvcDesktopToolChain(buildingOptions.Architecture, buildingOptions.Configuration, buildingOptions.TreatWarningsAsErrors, buildingOptions.AssemblyOutput, buildingOptions.UseDependenciesToolChain, buildingOptions.DisableExceptions, buildingOptions.ShowIncludes);
		}

		public override MonoSourceFileList GetMonoSourceFileList()
		{
			return new WindowsDesktopMonoSourceFileList();
		}

		public override MonoSourceFileList GetDebuggerMonoSourceFileList()
		{
			return new WindowsDebuggerMonoSourceFileList();
		}

		public override string BaselibToolchainName(Architecture architecture)
		{
			return $"win{architecture.Bits}";
		}
	}
}
