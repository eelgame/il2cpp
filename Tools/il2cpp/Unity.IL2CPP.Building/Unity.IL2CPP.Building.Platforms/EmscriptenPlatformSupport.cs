using System;
using Unity.IL2CPP.Building.BuildDescriptions;
using Unity.IL2CPP.Building.ToolChains;
using Unity.IL2CPP.Common;

namespace Unity.IL2CPP.Building.Platforms
{
	internal class EmscriptenPlatformSupport : PlatformSupport
	{
		public override string BaselibPlatformName { get; } = "WebGL";


		public override bool Supports(RuntimePlatform platform)
		{
			return platform is WebGLRuntimePlatform;
		}

		public override CppToolChain MakeCppToolChain(Architecture architecture, BuildConfiguration buildConfiguration, bool treatWarningsAsErrors)
		{
			return new EmscriptenToolChain(architecture, buildConfiguration);
		}

		public override CppToolChain MakeCppToolChain(BuildingOptions buildingOptions)
		{
			return new EmscriptenToolChain(buildingOptions.Architecture, buildingOptions.Configuration, buildingOptions.SetEnvironmentVariables, buildingOptions.UseDependenciesToolChain, buildingOptions.DisableExceptions, buildingOptions.EnableScriptDebugging, buildingOptions.DataFolder);
		}

		public override ProgramBuildDescription PostProcessProgramBuildDescription(ProgramBuildDescription programBuildDescription)
		{
			if (!(programBuildDescription is IL2CPPOutputBuildDescription buildDescription))
			{
				return programBuildDescription;
			}
			return new EmscriptenIL2CPPOutputBuildDescription(buildDescription);
		}

		public override Architecture GetSupportedArchitectureOfSameBitness(Architecture architecture)
		{
			if (architecture.Bits != 32)
			{
				throw new NotSupportedException($"Emscripten doesn't support {architecture.Bits}-bit architecture.");
			}
			return new EmscriptenJavaScriptArchitecture();
		}

		public override string BaselibToolchainName(Architecture architecture)
		{
			return "webgl_asmjs";
		}
	}
}
