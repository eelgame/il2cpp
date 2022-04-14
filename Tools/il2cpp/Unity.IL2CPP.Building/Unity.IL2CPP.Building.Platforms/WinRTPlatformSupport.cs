using System;
using Unity.IL2CPP.Building.BuildDescriptions;
using Unity.IL2CPP.Building.BuildDescriptions.Mono;
using Unity.IL2CPP.Building.ToolChains;
using Unity.IL2CPP.Common;

namespace Unity.IL2CPP.Building.Platforms
{
	internal class WinRTPlatformSupport : PlatformSupport
	{
		public override string BaselibPlatformName { get; } = "UniversalWindows";


		public override BaselibBuildType BaselibBuildType => BaselibBuildType.DynamicLibrary;

		public override bool Supports(RuntimePlatform platform)
		{
			return platform is WinRTRuntimePlatform;
		}

		public override ProgramBuildDescription PostProcessProgramBuildDescription(ProgramBuildDescription programBuildDescription)
		{
			if (!(programBuildDescription is IL2CPPOutputBuildDescription buildDescription))
			{
				return programBuildDescription;
			}
			return new WindowsIL2CPPOutputBuildDescription(buildDescription);
		}

		public override CppToolChain MakeCppToolChain(Architecture architecture, BuildConfiguration buildConfiguration, bool treatWarningsAsErrors)
		{
			return new MsvcWinRtToolChain(architecture, buildConfiguration, treatWarningsAsErrors, assemblyOutput: false, useDependenciesToolChain: false);
		}

		public override CppToolChain MakeCppToolChain(Architecture architecture, BuildConfiguration buildConfiguration, bool treatWarningsAsErrors, bool assemblyOutput)
		{
			return new MsvcWinRtToolChain(architecture, buildConfiguration, treatWarningsAsErrors, assemblyOutput, useDependenciesToolChain: false);
		}

		public override CppToolChain MakeCppToolChain(BuildingOptions buildingOptions)
		{
			return new MsvcWinRtToolChain(buildingOptions.Architecture, buildingOptions.Configuration, buildingOptions.TreatWarningsAsErrors, buildingOptions.AssemblyOutput, buildingOptions.UseDependenciesToolChain);
		}

		public override MonoSourceFileList GetMonoSourceFileList()
		{
			return new WinRTMonoSourceFileList();
		}

		public override MonoSourceFileList GetDebuggerMonoSourceFileList()
		{
			return new WinRTDebuggerMonoSourceFileList();
		}

		public override string BaselibToolchainName(Architecture architecture)
		{
			return "uap_" + BaseLibNameFor(architecture);
		}

		private static string BaseLibNameFor(Architecture architecture)
		{
			if (architecture is x86Architecture)
			{
				return "x86";
			}
			if (architecture is x64Architecture)
			{
				return "x64";
			}
			if (architecture is ARMv7Architecture)
			{
				return "arm";
			}
			if (architecture is ARM64Architecture)
			{
				return "arm64";
			}
			throw new NotImplementedException($"Unknown architecture for WinRT builds: '{architecture}'");
		}
	}
}
