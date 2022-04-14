using Unity.IL2CPP.Building.BuildDescriptions.Mono;
using Unity.IL2CPP.Building.ToolChains;
using Unity.IL2CPP.Common;

namespace Unity.IL2CPP.Building.Platforms
{
	internal class LinuxPlatformSupport : PlatformSupport
	{
		public override string BaselibPlatformName { get; } = "Linux";


		public override bool Supports(RuntimePlatform platform)
		{
			return platform is LinuxRuntimePlatform;
		}

		public override CppToolChain MakeCppToolChain(Architecture architecture, BuildConfiguration buildConfiguration, bool treatWarningsAsErrors)
		{
			return new LinuxClangToolChain(new BuildingOptions
			{
				Architecture = architecture,
				Configuration = buildConfiguration,
				TreatWarningsAsErrors = treatWarningsAsErrors
			});
		}

		public override CppToolChain MakeCppToolChain(BuildingOptions buildingOptions)
		{
			return new LinuxClangToolChain(buildingOptions);
		}

		public override MonoSourceFileList GetMonoSourceFileList()
		{
			return new LinuxMonoSourceFileList();
		}

		public override MonoSourceFileList GetDebuggerMonoSourceFileList()
		{
			return new LinuxDebuggerMonoSourceFileList();
		}

		public override string BaselibToolchainName(Architecture architecture)
		{
			return $"linux{architecture.Bits}";
		}
	}
}
