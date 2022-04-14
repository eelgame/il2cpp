using System;
using Unity.IL2CPP.Building.BuildDescriptions;
using Unity.IL2CPP.Building.BuildDescriptions.Mono;
using Unity.IL2CPP.Building.ToolChains;
using Unity.IL2CPP.Common;

namespace Unity.IL2CPP.Building.Platforms
{
	internal class AndroidPlatformSupport : PlatformSupport
	{
		public override string BaselibPlatformName { get; } = "Android";


		public override bool Supports(RuntimePlatform platform)
		{
			return platform is AndroidRuntimePlatform;
		}

		public override CppToolChain MakeCppToolChain(Architecture architecture, BuildConfiguration buildConfiguration, bool treatWarningsAsErrors)
		{
			return new AndroidToolChain(architecture, buildConfiguration, treatWarningsAsErrors, assemblyOutput: false, useDependenciesToolChain: false);
		}

		public override CppToolChain MakeCppToolChain(Architecture architecture, BuildConfiguration buildConfiguration, bool treatWarningsAsErrors, bool assemblyOutput)
		{
			return new AndroidToolChain(architecture, buildConfiguration, treatWarningsAsErrors, assemblyOutput, useDependenciesToolChain: false);
		}

		public override CppToolChain MakeCppToolChain(BuildingOptions buildingOptions)
		{
			return new AndroidToolChain(buildingOptions.Architecture, buildingOptions.Configuration, buildingOptions.TreatWarningsAsErrors, buildingOptions.AssemblyOutput, buildingOptions.UseDependenciesToolChain, buildingOptions.ToolChainPath);
		}

		public override ProgramBuildDescription PostProcessProgramBuildDescription(ProgramBuildDescription programBuildDescription)
		{
			IL2CPPOutputBuildDescription iL2CPPOutputBuildDescription = programBuildDescription as IL2CPPOutputBuildDescription;
			if (programBuildDescription is UnitTestPlusPlusBuildDescription other)
			{
				return new AndroidUnitTestPlusPlusBuildDescription(other);
			}
			if (iL2CPPOutputBuildDescription != null)
			{
				return new AndroidCppRunnerBuildDescription(iL2CPPOutputBuildDescription);
			}
			return base.PostProcessProgramBuildDescription(programBuildDescription);
		}

		public override Architecture GetSupportedArchitectureOfSameBitness(Architecture architecture)
		{
			if (architecture.Bits != 32)
			{
				throw new NotSupportedException($"Android doesn't support {architecture.Bits}-bit architecture.");
			}
			return new ARMv7Architecture();
		}

		public override MonoSourceFileList GetMonoSourceFileList()
		{
			return new AndroidMonoSourceFileList();
		}

		public override MonoSourceFileList GetDebuggerMonoSourceFileList()
		{
			return new AndroidDebuggerMonoSourceFileList();
		}

		public override string BaselibToolchainName(Architecture architecture)
		{
			return "android_" + BaseLibNameFor(architecture);
		}

		private static string BaseLibNameFor(Architecture architecture)
		{
			if (architecture is x86Architecture)
			{
				return "x86";
			}
			if (architecture is ARMv7Architecture)
			{
				return "armv7";
			}
			if (architecture is ARM64Architecture)
			{
				return "aarch64";
			}
			throw new NotImplementedException($"Unknown architecture for Android builds: '{architecture}'");
		}
	}
}
