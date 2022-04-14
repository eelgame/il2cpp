using System;
using System.Collections.Generic;
using NiceIO;
using Unity.IL2CPP.Common;

namespace Unity.IL2CPP.Building.ToolChains.MsvcVersions
{
	internal class Msvc14Installation : MsvcInstallation
	{
		private readonly string _sdkVersion;

		private readonly int _sdkBuildVersion;

		private readonly NPath _netfxsdkDir;

		private readonly List<NPath> _sdkBinDirectories = new List<NPath>();

		private readonly NPath _sdkUnionMetadataDirectory;

		public override int WindowsSDKBuildVersion => _sdkBuildVersion;

		public override IEnumerable<Type> SupportedArchitectures => new Type[3]
		{
			typeof(x86Architecture),
			typeof(ARMv7Architecture),
			typeof(x64Architecture)
		};

		public Msvc14Installation(NPath visualStudioDir)
			: this(visualStudioDir, WindowsSDKs.GetWindows10SDKDirectoryAndVersion(), WindowsSDKs.GetDotNetFrameworkSDKDirectory())
		{
		}

		private Msvc14Installation(NPath visualStudioDir, Tuple<NPath, string> sdkDirAndVersion, NPath netfxsdkDir)
			: this(visualStudioDir, sdkDirAndVersion.Item1, sdkDirAndVersion.Item2, netfxsdkDir)
		{
		}

		protected Msvc14Installation(NPath visualStudioDir, NPath sdkDirectory, string sdkVersion, NPath netfxsdkDir)
			: base(new Version(14, 0), visualStudioDir)
		{
			base.SDKDirectory = sdkDirectory;
			_sdkVersion = sdkVersion;
			_netfxsdkDir = netfxsdkDir;
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

		protected internal override IEnumerable<NPath> GetSdkIncludeDirectories(Architecture architecture)
		{
			NPath includeDirectory = base.SDKDirectory.Combine("Include").Combine(_sdkVersion);
			yield return includeDirectory.Combine("shared");
			yield return includeDirectory.Combine("um");
			yield return includeDirectory.Combine("winrt");
			yield return includeDirectory.Combine("ucrt");
		}

		protected internal override IEnumerable<NPath> GetVcIncludeDirectories(Architecture architecture)
		{
			yield return VisualStudioDirectory.Combine("VC", "include");
		}

		protected internal override IEnumerable<NPath> GetSdkLibDirectories(Architecture architecture, string sdkSubset = null)
		{
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
			throw new NotSupportedException($"Architecture {architecture} is not supported by MsvcToolChain!");
		}

		protected internal override IEnumerable<NPath> GetVcLibDirectories(Architecture architecture, string sdkSubset = null)
		{
			NPath nPath = VisualStudioDirectory.Combine("VC", "lib");
			if (sdkSubset != null)
			{
				nPath = nPath.Combine(sdkSubset);
			}
			if (architecture is x86Architecture)
			{
				yield return nPath;
				yield break;
			}
			if (architecture is x64Architecture)
			{
				yield return nPath.Combine("amd64");
				yield break;
			}
			if (architecture is ARMv7Architecture)
			{
				yield return nPath.Combine("arm");
				yield break;
			}
			throw new NotSupportedException($"Architecture {architecture} is not supported by MsvcToolChain!");
		}

		public override bool HasMetadataDirectories()
		{
			return true;
		}

		internal override IEnumerable<NPath> GetPlatformMetadataReferences()
		{
			yield return VisualStudioDirectory.Combine("VC", "vcpackages", "platform.winmd");
		}

		internal override IEnumerable<NPath> GetWindowsMetadataReferences()
		{
			yield return GetUnionMetadataDirectory().Combine("windows.winmd");
		}

		internal override NPath GetUnionMetadataDirectory()
		{
			return _sdkUnionMetadataDirectory;
		}
	}
}
