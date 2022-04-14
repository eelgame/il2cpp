using System;
using System.Collections.Generic;
using Unity.IL2CPP.Common;

namespace Unity.IL2CPP.Building.ToolChains.MsvcVersions
{
	internal class DependenciesMsvcInstallation : MsvcSystemInstallationBase
	{
		private static readonly Dictionary<Type, VCPaths> _vcPaths;

		private static Unity.IL2CPP.Common.ToolChains.WindowsDependenciesToolChain DependenciesToolChain => Unity.IL2CPP.Common.ToolChains.Windows;

		static DependenciesMsvcInstallation()
		{
			_vcPaths = new Dictionary<Type, VCPaths>();
			_vcPaths.Add(typeof(x86Architecture), new VCPaths(DependenciesToolChain.VCDirectory.Combine("bin", "Hostx64", "x86"), DependenciesToolChain.VCDirectory.Combine("include"), DependenciesToolChain.VCDirectory.Combine("lib", "x86")));
			_vcPaths.Add(typeof(x64Architecture), new VCPaths(DependenciesToolChain.VCDirectory.Combine("bin", "Hostx64", "x64"), DependenciesToolChain.VCDirectory.Combine("include"), DependenciesToolChain.VCDirectory.Combine("lib", "x64")));
			_vcPaths.Add(typeof(ARMv7Architecture), new VCPaths(DependenciesToolChain.VCDirectory.Combine("bin", "Hostx64", "arm"), DependenciesToolChain.VCDirectory.Combine("include"), DependenciesToolChain.VCDirectory.Combine("lib", "arm")));
			_vcPaths.Add(typeof(ARM64Architecture), new VCPaths(DependenciesToolChain.VCDirectory.Combine("bin", "Hostx64", "arm64"), DependenciesToolChain.VCDirectory.Combine("include"), DependenciesToolChain.VCDirectory.Combine("lib", "arm64")));
		}

		public DependenciesMsvcInstallation()
			: base(DependenciesToolChain.SDKDirectory, DependenciesToolChain.SDKVersion, null, _vcPaths, "x64", DependenciesToolChain.VisualStudioMajorVersion)
		{
		}
	}
}
