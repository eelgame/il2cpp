using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;
using NiceIO;
using Unity.IL2CPP.Common;

namespace Unity.IL2CPP.Building.ToolChains.MsvcVersions
{
	public abstract class MsvcInstallation
	{
		private static Dictionary<Version, MsvcInstallation> _installations;

		private readonly bool _use64BitTools;

		protected virtual NPath VisualStudioDirectory { get; set; }

		public NPath SDKDirectory { get; set; }

		public abstract IEnumerable<Type> SupportedArchitectures { get; }

		public Version Version { get; set; }

		public virtual int WindowsSDKBuildVersion => 0;

		public virtual IEnumerable<NPath> GetIncludeDirectories(Unity.IL2CPP.Common.Architecture architecture)
		{
			return GetVcIncludeDirectories(architecture).Concat(GetSdkIncludeDirectories(architecture));
		}

		public virtual IEnumerable<NPath> GetLibDirectories(Unity.IL2CPP.Common.Architecture architecture, string sdkSubset = null)
		{
			return GetVcLibDirectories(architecture, sdkSubset).Concat(GetSdkLibDirectories(architecture, sdkSubset));
		}

		protected internal abstract IEnumerable<NPath> GetSdkIncludeDirectories(Unity.IL2CPP.Common.Architecture architecture);

		protected internal abstract IEnumerable<NPath> GetSdkLibDirectories(Unity.IL2CPP.Common.Architecture architecture, string sdkSubset = null);

		protected internal abstract IEnumerable<NPath> GetVcIncludeDirectories(Unity.IL2CPP.Common.Architecture architecture);

		protected internal abstract IEnumerable<NPath> GetVcLibDirectories(Unity.IL2CPP.Common.Architecture architecture, string sdkSubset = null);

		public virtual bool HasMetadataDirectories()
		{
			return false;
		}

		protected virtual IEnumerable<NPath> GetSDKBinDirectories()
		{
			yield return SDKDirectory.Combine("bin");
		}

		internal virtual IEnumerable<NPath> GetPlatformMetadataReferences()
		{
			throw new NotSupportedException($"{GetType().Name} does not support platform metadata");
		}

		internal virtual IEnumerable<NPath> GetWindowsMetadataReferences()
		{
			throw new NotSupportedException($"{GetType().Name} does not support windows metadata");
		}

		internal virtual NPath GetUnionMetadataDirectory()
		{
			throw new NotSupportedException($"{GetType().Name} does not support union metadata");
		}

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool IsWow64Process(IntPtr hProcess, out bool wow64Process);

		private static bool CanRun64BitProcess()
		{
			if (IntPtr.Size != 4)
			{
				return true;
			}
			using (Process process = Process.GetCurrentProcess())
			{
				bool wow64Process;
				return IsWow64Process(process.Handle, out wow64Process) && wow64Process;
			}
		}

		protected MsvcInstallation(Version visualStudioVersion, NPath visualStudioDir, bool use64BitTools = true)
		{
			VisualStudioDirectory = visualStudioDir;
			Version = visualStudioVersion;
			_use64BitTools = use64BitTools && CanRun64BitProcess();
		}

		protected MsvcInstallation(Version visualStudioVersion)
		{
			VisualStudioDirectory = GetVisualStudioInstallationFolder(visualStudioVersion);
			Version = visualStudioVersion;
			_use64BitTools = CanRun64BitProcess();
		}

		public virtual string GetPathEnvVariable(Unity.IL2CPP.Common.Architecture architecture)
		{
			List<NPath> list = new List<NPath>();
			if (architecture is x86Architecture)
			{
				if (_use64BitTools)
				{
					list.Add(VisualStudioDirectory.Combine("VC", "bin", "amd64_x86"));
					list.Add(VisualStudioDirectory.Combine("VC", "bin", "amd64"));
					if (SDKDirectory != null)
					{
						list.AddRange(from d in GetSDKBinDirectories()
							select d.Combine("x64"));
					}
				}
				else
				{
					list.Add(VisualStudioDirectory.Combine("VC", "bin"));
				}
				if (SDKDirectory != null)
				{
					list.AddRange(from d in GetSDKBinDirectories()
						select d.Combine("x86"));
				}
			}
			else if (architecture is ARMv7Architecture)
			{
				if (_use64BitTools)
				{
					list.Add(VisualStudioDirectory.Combine("VC", "bin", "amd64_arm"));
					list.Add(VisualStudioDirectory.Combine("VC", "bin", "amd64"));
					if (SDKDirectory != null)
					{
						list.AddRange(from d in GetSDKBinDirectories()
							select d.Combine("x64"));
					}
				}
				else
				{
					list.Add(VisualStudioDirectory.Combine("VC", "bin", "x86_arm"));
					list.Add(VisualStudioDirectory.Combine("VC", "bin"));
				}
				if (SDKDirectory != null)
				{
					list.AddRange(from d in GetSDKBinDirectories()
						select d.Combine("x86"));
				}
			}
			else
			{
				if (!(architecture is x64Architecture))
				{
					throw new NotSupportedException("'" + architecture.Name + "' architecture is not supported.");
				}
				list.Add(VisualStudioDirectory.Combine("VC", "bin", "amd64"));
				if (SDKDirectory != null)
				{
					list.AddRange(from d in GetSDKBinDirectories()
						select d.Combine("x64"));
					list.AddRange(from d in GetSDKBinDirectories()
						select d.Combine("x86"));
				}
			}
			return list.Select((NPath p) => p.ToString()).AggregateWith(";");
		}

		public virtual NPath GetVSToolPath(Unity.IL2CPP.Common.Architecture architecture, string toolName)
		{
			NPath nPath = VisualStudioDirectory.Combine("VC", "bin");
			if (architecture is x86Architecture)
			{
				if (!_use64BitTools)
				{
					return nPath.Combine(toolName);
				}
				return nPath.Combine("amd64_x86", toolName);
			}
			if (architecture is x64Architecture)
			{
				return nPath.Combine("amd64", toolName);
			}
			if (architecture is ARMv7Architecture)
			{
				if (!_use64BitTools)
				{
					return nPath.Combine("x86_arm", toolName);
				}
				return nPath.Combine("amd64_arm", toolName);
			}
			throw new NotSupportedException("Can't find MSVC tool for " + architecture);
		}

		public virtual NPath GetSDKToolPath(string toolName)
		{
			Unity.IL2CPP.Common.Architecture bestThisMachineCanRun = Unity.IL2CPP.Common.Architecture.BestThisMachineCanRun;
			return GetSDKBinDirectoryFor(bestThisMachineCanRun, toolName);
		}

		public NPath GetSDKBinDirectoryFor(Unity.IL2CPP.Common.Architecture architecture, NPath tool)
		{
			NPath nPath;
			if (architecture is x86Architecture)
			{
				nPath = "x86";
			}
			else if (architecture is x64Architecture)
			{
				nPath = "x64";
			}
			else if (architecture is ARMv7Architecture)
			{
				nPath = "arm";
			}
			else
			{
				if (!(architecture is ARM64Architecture))
				{
					throw new NotSupportedException($"Architecture {architecture} is not supported by MsvcToolChain!");
				}
				nPath = "arm64";
			}
			foreach (NPath sDKBinDirectory in GetSDKBinDirectories())
			{
				NPath nPath2 = sDKBinDirectory.Combine(nPath, tool);
				if (nPath2.FileExists())
				{
					return nPath2;
				}
			}
			throw new NotSupportedException("Can't find SDK bin path for " + tool);
		}

		public bool CanBuildCode(Unity.IL2CPP.Common.Architecture architecture)
		{
			return GetReasonCannotBuildCode(architecture) == null;
		}

		private string GetReasonCannotBuildCode(Unity.IL2CPP.Common.Architecture architecture)
		{
			if (SDKDirectory == null || !SDKDirectory.DirectoryExists())
			{
				return "Windows 10 SDK is not installed. You can install from here: https://developer.microsoft.com/en-us/windows/downloads/windows-10-sdk/";
			}
			if (SupportedArchitectures.All((Type a) => !a.Equals(architecture.GetType())))
			{
				return "architecture " + architecture.Name + " is not supported by this Visual Studio version";
			}
			NPath vSToolPath = GetVSToolPath(architecture, "cl.exe");
			NPath vSToolPath2 = GetVSToolPath(architecture, "link.exe");
			if (vSToolPath == null || !vSToolPath.FileExists())
			{
				return "C++ tool components for " + architecture.Name + " are not installed";
			}
			if (vSToolPath2 == null || !vSToolPath2.FileExists())
			{
				return "C++ tool components for " + architecture.Name + " are not installed";
			}
			return null;
		}

		static MsvcInstallation()
		{
			_installations = new Dictionary<Version, MsvcInstallation>();
			Version version = new Version(14, 0);
			NPath visualStudioInstallationFolder = GetVisualStudioInstallationFolder(version);
			if (visualStudioInstallationFolder != null)
			{
				_installations.Add(version, new Msvc14Installation(visualStudioInstallationFolder));
			}
			foreach (MsvcSystemInstallation item in MsvcSystemInstallation.GetAllInstalled())
			{
				_installations.Add(item.Version, item);
			}
		}

		protected static NPath GetVisualStudioInstallationFolder(Version version)
		{
			if (!PlatformUtils.IsWindows())
			{
				return null;
			}
			NPath nPath = null;
			RegistryKey registryKey = Registry.CurrentUser.OpenSubKey($"SOFTWARE\\Microsoft\\VisualStudio\\{version.Major}.{version.Minor}_Config");
			if (registryKey != null)
			{
				string text = (string)registryKey.GetValue("InstallDir");
				if (!string.IsNullOrEmpty(text))
				{
					NPath parent = new NPath(text).Parent.Parent;
					if (parent.DirectoryExists())
					{
						nPath = parent;
					}
				}
			}
			if (nPath == null)
			{
				string environmentVariable = Environment.GetEnvironmentVariable($"VS{version.Major}{version.Minor}COMNTOOLS");
				if (!string.IsNullOrEmpty(environmentVariable))
				{
					nPath = environmentVariable.ToNPath().Parent.Parent;
				}
			}
			if (nPath != null && nPath.DirectoryExists())
			{
				return nPath;
			}
			return null;
		}

		public static MsvcInstallation GetDependenciesInstallation()
		{
			Unity.IL2CPP.Common.ToolChains.Windows.AssertReadyToUse();
			return new DependenciesMsvcInstallation();
		}

		public static MsvcInstallation GetWindowsGamesDependenciesInstallation()
		{
			Unity.IL2CPP.Common.ToolChains.Windows.AssertReadyToUse();
			Unity.IL2CPP.Common.ToolChains.WindowsGamesSDK.AssertReadyToUse();
			return new WindowsGamesDependenciesMsvcInstallation();
		}

		public static MsvcInstallation GetLatestFunctionalInstallation(Unity.IL2CPP.Common.Architecture architecture)
		{
			KeyValuePair<Version, MsvcInstallation> keyValuePair = (from kvp in _installations
				orderby kvp.Key.Major descending, kvp.Key.Minor descending
				where kvp.Value.CanBuildCode(architecture)
				select kvp).FirstOrDefault();
			if (keyValuePair.Value != null)
			{
				return keyValuePair.Value;
			}
			throw new Exception("No MSVC installations were found on the machine!");
		}

		public static MsvcInstallation GetLatestFunctionalInstallationAtLeast(Version version, Unity.IL2CPP.Common.Architecture architecture)
		{
			KeyValuePair<Version, MsvcInstallation> keyValuePair = (from kvp in _installations
				orderby kvp.Key.Major descending, kvp.Key.Minor descending
				where kvp.Key >= version && kvp.Value.CanBuildCode(architecture)
				select kvp).FirstOrDefault();
			if (keyValuePair.Value != null)
			{
				return keyValuePair.Value;
			}
			throw new Exception($"MSVC Installation version {version.Major}.{version.Minor} or later is not installed on current machine!");
		}

		public static MsvcInstallation GetLatestFunctionalInstallationInRange(Version earliestVersion, Version latestVersion, Unity.IL2CPP.Common.Architecture architecture)
		{
			KeyValuePair<Version, MsvcInstallation> keyValuePair = (from kvp in _installations
				orderby kvp.Key.Major descending, kvp.Key.Minor descending
				where kvp.Key <= latestVersion && kvp.Key >= earliestVersion && kvp.Value.CanBuildCode(architecture)
				select kvp).FirstOrDefault();
			if (keyValuePair.Value != null)
			{
				return keyValuePair.Value;
			}
			throw new Exception($"MSVC Installation version between {earliestVersion.Major}.{earliestVersion.Minor} and {latestVersion.Major}.{latestVersion.Minor} is not installed on current machine!");
		}

		public static MsvcInstallation GetExactFunctionalInstallation(Version version, Unity.IL2CPP.Common.Architecture architecture)
		{
			if (_installations.TryGetValue(version, out var value) && value.CanBuildCode(architecture))
			{
				return value;
			}
			throw new Exception($"MSVC Installation version {version.Major}.{version.Minor} cannot build C++ code because {GetReasonMsvcInstallationCannotBuild(version, architecture)}.");
		}

		public static string GetMsvcVersionRequirementsForBuildingAndReasonItCannotBuild(Version version, Unity.IL2CPP.Common.Architecture architecture)
		{
			StringBuilder stringBuilder = new StringBuilder();
			switch (version.Major)
			{
			case 14:
				stringBuilder.AppendLine("    Visual Studio 2015 with C++ compilers and Windows 10 SDK (it cannot build C++ code because " + GetReasonMsvcInstallationCannotBuild(version, architecture) + ")");
				stringBuilder.AppendLine("        Visual Studio 2015 installation is found by looking at \"SOFTWARE\\Microsoft\\VisualStudio\\14.0_Config\\InstallDir\" in the registry");
				stringBuilder.AppendLine("        Windows 10 SDK is found by looking at \"SOFTWARE\\Wow6432Node\\Microsoft\\Microsoft SDKs\\Windows\\v10.0\\InstallationFolder\" in the registry");
				break;
			case 15:
				stringBuilder.AppendLine("    Visual Studio 2017 (or newer) with C++ compilers and Windows 10 SDK (it cannot build C++ code because " + GetReasonMsvcInstallationCannotBuild(version, architecture) + ")");
				stringBuilder.AppendLine("        Visual Studio 2017 (or newer) installation is found using Microsoft.VisualStudio.Setup.Configuration COM APIs");
				stringBuilder.AppendLine("        Windows 10 SDK is found by looking at \"SOFTWARE\\Wow6432Node\\Microsoft\\Microsoft SDKs\\Windows\\v10.0\\InstallationFolder\" in the registry");
				break;
			default:
				throw new InvalidOperationException("Unknown MSVC version: " + version);
			}
			return stringBuilder.ToString();
		}

		public static bool IsMsvcInstallationAvailable(Version version)
		{
			return _installations.ContainsKey(version);
		}

		public static string GetReasonMsvcInstallationCannotBuild(Version version, Unity.IL2CPP.Common.Architecture architecture)
		{
			if (!_installations.TryGetValue(version, out var value))
			{
				return "it is not installed or missing C++ workload component";
			}
			return value.GetReasonMsvcInstallationCannotBuild(architecture);
		}

		public string GetReasonMsvcInstallationCannotBuild(Unity.IL2CPP.Common.Architecture architecture)
		{
			if (CanBuildCode(architecture))
			{
				throw new InvalidOperationException($"Msvc version {Version.Major}.{Version.Minor} can actually build code, therefore it is invalid to ask why it cannot do so.");
			}
			return GetReasonCannotBuildCode(architecture);
		}
	}
}
