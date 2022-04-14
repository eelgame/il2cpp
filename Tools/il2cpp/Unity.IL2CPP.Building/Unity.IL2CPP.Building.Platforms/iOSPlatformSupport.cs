using System;
using Unity.IL2CPP.Building.BuildDescriptions;
using Unity.IL2CPP.Building.BuildDescriptions.Mono;
using Unity.IL2CPP.Building.ToolChains;
using Unity.IL2CPP.Common;

namespace Unity.IL2CPP.Building.Platforms
{
	internal class iOSPlatformSupport : PlatformSupport
	{
		public override string BaselibPlatformName { get; } = "IOS";


		public override bool Supports(RuntimePlatform platform)
		{
			return platform is iOSRuntimePlatform;
		}

		public override CppToolChain MakeCppToolChain(Architecture architecture, BuildConfiguration buildConfiguration, bool treatWarningsAsErrors)
		{
			return new iOSClangToolChain(architecture, buildConfiguration, treatWarningsAsErrors, useDependenciesToolChain: false);
		}

		public override CppToolChain MakeCppToolChain(BuildingOptions buildingOptions)
		{
			return new iOSClangToolChain(buildingOptions.Architecture, buildingOptions.Configuration, buildingOptions.TreatWarningsAsErrors, buildingOptions.UseDependenciesToolChain);
		}

		public override Architecture GetSupportedArchitectureOfSameBitness(Architecture architecture)
		{
			if (architecture.Bits == 64)
			{
				return new ARM64Architecture();
			}
			return new ARMv7Architecture();
		}

		public override ProgramBuildDescription PostProcessProgramBuildDescription(ProgramBuildDescription programBuildDescription)
		{
			if (!(programBuildDescription is IL2CPPOutputBuildDescription buildDescription))
			{
				return programBuildDescription;
			}
			return new iOSIL2CPPOutputBuildDescription(buildDescription);
		}

		public override string BaselibToolchainName(Architecture architecture)
		{
			return "ios_" + BaseLibNameFor(architecture);
		}

		private static string BaseLibNameFor(Architecture architecture)
		{
			if (architecture is x86Architecture)
			{
				return "x86";
			}
			if (architecture is x64Architecture)
			{
				return "x86_x64";
			}
			if (architecture is ARMv7Architecture)
			{
				return "armv7";
			}
			if (architecture is ARM64Architecture)
			{
				return "aarch64";
			}
			throw new NotImplementedException($"Unknown architecture for iOS builds: '{architecture}'");
		}

		public override MonoSourceFileList GetDebuggerMonoSourceFileList()
		{
			return new iOSDebuggerMonoSourceFileList();
		}
	}
}
