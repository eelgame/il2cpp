using System;
using System.Collections.Generic;
using Unity.IL2CPP.Common;

namespace Unity.IL2CPP.Building.ToolChains.MsvcVersions
{
	internal class WindowsGamesDependenciesMsvcInstallation : MsvcSystemInstallationBase
	{
		private static readonly Dictionary<Type, VCPaths> _vcPaths;

		private static Unity.IL2CPP.Common.ToolChains.VS2019DependenciesToolChain DependenciesToolChain => Unity.IL2CPP.Common.ToolChains.VS2019;

		private static Unity.IL2CPP.Common.ToolChains.WindowsGamesSDKDependenciesToolChain WinGamesSdkDependenciesToolChain => Unity.IL2CPP.Common.ToolChains.WindowsGamesSDK;

		static WindowsGamesDependenciesMsvcInstallation()
		{
			_vcPaths = new Dictionary<Type, VCPaths>();
			_vcPaths.Add(typeof(x64Architecture), new VCPaths(DependenciesToolChain.VCDirectory.Combine("bin", "Hostx64", "x64"), DependenciesToolChain.VCDirectory.Combine("include"), DependenciesToolChain.VCDirectory.Combine("lib", "x64"), DependenciesToolChain.VCDirectory.Combine("Redist")));
		}

		public WindowsGamesDependenciesMsvcInstallation()
			: base(WinGamesSdkDependenciesToolChain.SDKDirectory, WinGamesSdkDependenciesToolChain.SDKVersion, null, _vcPaths, "x64", DependenciesToolChain.VisualStudioMajorVersion)
		{
		}
	}
}
