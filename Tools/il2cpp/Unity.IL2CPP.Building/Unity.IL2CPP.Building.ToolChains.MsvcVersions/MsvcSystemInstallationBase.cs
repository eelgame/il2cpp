using System;
using System.Collections.Generic;
using System.Linq;
using NiceIO;
using Unity.IL2CPP.Common;

namespace Unity.IL2CPP.Building.ToolChains.MsvcVersions
{
	internal class MsvcSystemInstallationBase : MsvcInstallation
	{
		private readonly string _sdkVersion;

		private readonly int _sdkBuildVersion;

		private readonly NPath _netfxsdkDir;

		private readonly List<NPath> _sdkBinDirectories = new List<NPath>();

		private readonly NPath _sdkUnionMetadataDirectory;

		private readonly Dictionary<Type, VCPaths> _vcPaths;

		private readonly string _hostDirectoryNativeFolder;

		protected override NPath VisualStudioDirectory
		{
			get
			{
				throw new NotSupportedException("Msvc15Installation does not support VisualStudioDirectory property!");
			}
			set
			{
			}
		}

		public override IEnumerable<Type> SupportedArchitectures => _vcPaths.Keys;

		public override int WindowsSDKBuildVersion => _sdkBuildVersion;

		public override bool HasMetadataDirectories()
		{
			return true;
		}

		public MsvcSystemInstallationBase(NPath sdkDirectory, string sdkVersion, NPath netfxsdkDir, Dictionary<Type, VCPaths> vcPaths, string hostDirectoryNativeFolder, int vsMajorVersion)
			: base(new Version(vsMajorVersion, 0))
		{
			base.SDKDirectory = sdkDirectory;
			_sdkVersion = sdkVersion;
			_netfxsdkDir = netfxsdkDir;
			_vcPaths = vcPaths;
			_hostDirectoryNativeFolder = hostDirectoryNativeFolder;
			if (base.SDKDirectory != null)
			{
				NPath nPath = base.SDKDirectory.Combine("bin");
				NPath nPath2 = nPath.Combine(_sdkVersion);
				if (nPath2.DirectoryExists())
				{
					_sdkBinDirectories.Add(nPath2);
				}
				_sdkBinDirectories.Add(nPath);
				NPath nPath3 = base.SDKDirectory.Combine("UnionMetadata");
				NPath nPath4 = nPath3.Combine(_sdkVersion);
				_sdkUnionMetadataDirectory = (nPath4.DirectoryExists() ? nPath4 : nPath3);
				_sdkBuildVersion = Version.Parse(_sdkVersion).Build;
			}
		}

		protected override IEnumerable<NPath> GetSDKBinDirectories()
		{
			return _sdkBinDirectories;
		}

		internal override IEnumerable<NPath> GetPlatformMetadataReferences()
		{
			ThrowIfArchitectureNotInstalled(new x86Architecture());
			yield return _vcPaths[typeof(x86Architecture)].LibPath.Combine("store", "references", "platform.winmd");
		}

		internal override IEnumerable<NPath> GetWindowsMetadataReferences()
		{
			yield return GetUnionMetadataDirectory().Combine("windows.winmd");
		}

		internal override NPath GetUnionMetadataDirectory()
		{
			return _sdkUnionMetadataDirectory;
		}

		protected internal override IEnumerable<NPath> GetSdkIncludeDirectories(Architecture architecture)
		{
			ThrowIfArchitectureNotInstalled(architecture);
			NPath includeDirectory = base.SDKDirectory.Combine("Include").Combine(_sdkVersion);
			yield return includeDirectory.Combine("shared");
			yield return includeDirectory.Combine("um");
			yield return includeDirectory.Combine("winrt");
			yield return includeDirectory.Combine("ucrt");
		}

		protected internal override IEnumerable<NPath> GetVcIncludeDirectories(Architecture architecture)
		{
			ThrowIfArchitectureNotInstalled(architecture);
			yield return _vcPaths[architecture.GetType()].IncludePath;
		}

		protected internal override IEnumerable<NPath> GetSdkLibDirectories(Architecture architecture, string sdkSubset = null)
		{
			ThrowIfArchitectureNotInstalled(architecture);
			NPath libDirectory = base.SDKDirectory.Combine("Lib").Combine(_sdkVersion);
			if (architecture is x86Architecture)
			{
				yield return libDirectory.Combine("um", "x86");
				yield return libDirectory.Combine("ucrt", "x86");
				if (_netfxsdkDir != null)
				{
					yield return _netfxsdkDir.Combine("lib", "um", "x86");
				}
				yield break;
			}
			if (architecture is x64Architecture)
			{
				yield return libDirectory.Combine("um", "x64");
				yield return libDirectory.Combine("ucrt", "x64");
				if (_netfxsdkDir != null)
				{
					yield return _netfxsdkDir.Combine("lib", "um", "x64");
				}
				yield break;
			}
			if (architecture is ARMv7Architecture)
			{
				yield return libDirectory.Combine("um", "arm");
				yield return libDirectory.Combine("ucrt", "arm");
				if (_netfxsdkDir != null)
				{
					yield return _netfxsdkDir.Combine("lib", "um", "arm");
				}
				yield break;
			}
			if (architecture is ARM64Architecture)
			{
				yield return libDirectory.Combine("um", "arm64");
				yield return libDirectory.Combine("ucrt", "arm64");
				if (_netfxsdkDir != null)
				{
					yield return _netfxsdkDir.Combine("lib", "um", "arm64");
				}
				yield break;
			}
			throw new NotSupportedException($"Architecture {architecture} is not supported by MsvcToolChain!");
		}

		protected internal override IEnumerable<NPath> GetVcLibDirectories(Architecture architecture, string sdkSubset = null)
		{
			ThrowIfArchitectureNotInstalled(architecture);
			NPath libPath = _vcPaths[architecture.GetType()].LibPath;
			yield return (sdkSubset != null) ? libPath.Combine(sdkSubset) : libPath;
		}

		public override string GetPathEnvVariable(Architecture architecture)
		{
			ThrowIfArchitectureNotInstalled(architecture);
			List<NPath> list = new List<NPath>();
			list.AddRange(_sdkBinDirectories.Select((NPath d) => d.Combine("x64")));
			list.AddRange(_sdkBinDirectories.Select((NPath d) => d.Combine("x86")));
			list.Add(_vcPaths[architecture.GetType()].ToolsPath.Parent.Combine(_hostDirectoryNativeFolder));
			return list.Select((NPath p) => p.ToString()).AggregateWith(";");
		}

		public override NPath GetVSToolPath(Architecture architecture, string toolName)
		{
			ThrowIfArchitectureNotInstalled(architecture);
			return _vcPaths[architecture.GetType()].ToolsPath.Combine(toolName);
		}

		public NPath GetVcRedistPath(BuildConfiguration configuration, Architecture architecture, string redistName)
		{
			ThrowIfArchitectureNotInstalled(architecture);
			NPath nPath = _vcPaths[architecture.GetType()].RedistPath;
			if (nPath == null)
			{
				throw new NotSupportedException("Visual Studio 2017/2019 redistributable dlls are not installed.");
			}
			if (configuration == BuildConfiguration.Debug)
			{
				string text = "debug_nonredist";
				nPath = nPath.Parent.Combine(text, nPath.FileName);
			}
			return nPath.Combine(redistName);
		}

		private void ThrowIfArchitectureNotInstalled(Architecture architecture)
		{
			if (!_vcPaths.ContainsKey(architecture.GetType()))
			{
				throw new NotSupportedException("Visual Studio 2017 support for " + architecture.Name + " is not installed.");
			}
		}
	}
}
