using System;
using System.Collections.Generic;
using System.Linq;
using NiceIO;
using Unity.IL2CPP.Building.ToolChains.MsvcVersions.VisualStudioAPI;
using Unity.IL2CPP.Common;

namespace Unity.IL2CPP.Building.ToolChains.MsvcVersions
{
	internal class MsvcSystemInstallation : MsvcSystemInstallationBase
	{
		private class VCComponent
		{
			public readonly NPath InstallationPath;

			public readonly string VCToolsVersionString;

			public readonly Version VCToolsVersion;

			public readonly Version VisualStudioVersion;

			public VCComponent(ISetupPackageReference package, ISetupInstance2 setupInstance)
			{
				InstallationPath = setupInstance.GetInstallationPath().ToNPath();
				VCToolsVersionString = package.GetVersion();
				Version.TryParse(VCToolsVersionString, out VCToolsVersion);
				Version.TryParse(setupInstance.GetInstallationVersion(), out VisualStudioVersion);
			}
		}

		private class VCComponentVersionComparer : IComparer<VCComponent>
		{
			public int Compare(VCComponent x, VCComponent y)
			{
				if (x.VCToolsVersion == y.VCToolsVersion)
				{
					return 0;
				}
				if (x.VCToolsVersion == null)
				{
					return -1;
				}
				if (y.VCToolsVersion == null)
				{
					return 1;
				}
				if (!(x.VCToolsVersion < y.VCToolsVersion))
				{
					return 1;
				}
				return -1;
			}
		}

		private static Dictionary<int, Dictionary<Type, VCPaths>> _vcPaths;

		private static readonly string _hostDirectoryNativeFolder;

		private static readonly string _hostDirectory;

		private static readonly NPath _sdkDirectory;

		private static readonly string _sdkVersion;

		private static readonly NPath _netfxSDKDirectory;

		public static IEnumerable<MsvcSystemInstallation> GetAllInstalled()
		{
			foreach (int key in _vcPaths.Keys)
			{
				yield return new MsvcSystemInstallation(key);
			}
		}

		static MsvcSystemInstallation()
		{
			Architecture bestThisMachineCanRun = Architecture.BestThisMachineCanRun;
			if (bestThisMachineCanRun is x64Architecture)
			{
				_hostDirectoryNativeFolder = "x64";
				_hostDirectory = "HostX64";
			}
			else
			{
				if (!(bestThisMachineCanRun is x86Architecture))
				{
					throw new NotSupportedException($"Unknown host architecture: {bestThisMachineCanRun}");
				}
				_hostDirectoryNativeFolder = "x86";
				_hostDirectory = "HostX86";
			}
			_vcPaths = GetVCToolsPaths();
			_sdkDirectory = WindowsSDKs.GetWindows10SDKDirectory(out _sdkVersion);
			_netfxSDKDirectory = WindowsSDKs.GetDotNetFrameworkSDKDirectory();
		}

		public MsvcSystemInstallation(int vsMajorVersion)
			: base(_sdkDirectory, _sdkVersion, _netfxSDKDirectory, _vcPaths[vsMajorVersion], _hostDirectoryNativeFolder, vsMajorVersion)
		{
		}

		private static IEnumSetupInstances GetVSInstancesEnumerator()
		{
			SetupConfiguration setupConfiguration = null;
			try
			{
				setupConfiguration = new SetupConfiguration();
			}
			catch
			{
				return null;
			}
			try
			{
				return setupConfiguration.EnumInstances();
			}
			catch (Exception arg)
			{
				ConsoleOutput.Error.WriteLine($"Error in Msvc15SystemInstallation: failed to enumerate VS instances: {arg}");
				return null;
			}
		}

		private static bool GetNextVSInstance(IEnumSetupInstances enumerator, out object vsInstance)
		{
			int pceltFetched = 0;
			object[] array = new object[1];
			try
			{
				enumerator.Next(array.Length, array, out pceltFetched);
			}
			catch
			{
			}
			if (pceltFetched > 0)
			{
				vsInstance = array[0];
				return true;
			}
			vsInstance = null;
			return false;
		}

		private static Dictionary<int, Dictionary<Type, VCPaths>> GetVCToolsPaths()
		{
			Dictionary<int, Dictionary<Type, VCPaths>> result = new Dictionary<int, Dictionary<Type, VCPaths>>();
			IEnumSetupInstances vSInstancesEnumerator = GetVSInstancesEnumerator();
			if (vSInstancesEnumerator == null)
			{
				return result;
			}
			List<VCComponent> list = new List<VCComponent>();
			List<VCComponent> list2 = new List<VCComponent>();
			List<VCComponent> list3 = new List<VCComponent>();
			List<VCComponent> list4 = new List<VCComponent>();
			object vsInstance;
			while (GetNextVSInstance(vSInstancesEnumerator, out vsInstance))
			{
				ISetupInstance2 instance2 = vsInstance as ISetupInstance2;
				if (instance2 == null)
				{
					continue;
				}
				try
				{
					if ((instance2.GetState() & InstanceState.Local) != 0)
					{
						ISetupPackageReference[] packages = instance2.GetPackages();
						list.AddRange(from p in packages
							where p.GetId() == "Microsoft.VisualCpp.Tools." + _hostDirectory + ".TargetX86"
							select new VCComponent(p, instance2));
						list2.AddRange(from p in packages
							where p.GetId() == "Microsoft.VisualCpp.Tools." + _hostDirectory + ".TargetX64"
							select new VCComponent(p, instance2));
						list3.AddRange(from p in packages
							where p.GetId() == "Microsoft.VisualCpp.Tools." + _hostDirectory + ".TargetARM"
							select new VCComponent(p, instance2));
						list4.AddRange(from p in packages
							where p.GetId() == "Microsoft.VisualCpp.Tools." + _hostDirectory + ".TargetARM64"
							select new VCComponent(p, instance2));
					}
				}
				catch (Exception arg)
				{
					ConsoleOutput.Error.WriteLine($"Unexpected exception when trying to find Visual C++ directories: {arg}");
				}
			}
			AddVCPathsForArchitecture(result, new x86Architecture(), list, "x86");
			AddVCPathsForArchitecture(result, new x64Architecture(), list2, "x64");
			AddVCPathsForArchitecture(result, new ARMv7Architecture(), list3, "ARM");
			AddVCPathsForArchitecture(result, new ARM64Architecture(), list4, "ARM64");
			return result;
		}

		private static void AddVCPathsForArchitecture(Dictionary<int, Dictionary<Type, VCPaths>> result, Architecture architecture, List<VCComponent> vcComponents, string architectureFolder)
		{
			foreach (IGrouping<int, VCComponent> item in from component in vcComponents
				group component by component.VisualStudioVersion.Major)
			{
				VCComponent vCComponent = item.ToSortedCollection(new VCComponentVersionComparer()).LastOrDefault();
				if (vCComponent != null)
				{
					int key = item.Key;
					if (!result.TryGetValue(key, out var value))
					{
						value = new Dictionary<Type, VCPaths>();
						result.Add(key, value);
					}
					AddVCToolsForArchitecture(architecture, vCComponent.InstallationPath, vCComponent.VCToolsVersionString, architectureFolder, value);
				}
			}
		}

		private static void AddVCToolsForArchitecture(Architecture architecture, NPath installationPath, string vcToolsVersion, string architectureFolder, Dictionary<Type, VCPaths> vcToolsPaths)
		{
			NPath nPath = null;
			NPath nPath2 = installationPath.Combine("VC", "Auxiliary", "Build", "Microsoft.VCToolsVersion.default.txt");
			if (nPath2.FileExists())
			{
				string text = null;
				try
				{
					text = nPath2.ReadAllText().Trim();
				}
				catch
				{
				}
				if (text != null)
				{
					nPath = installationPath.Combine("VC", "Tools", "MSVC", text);
				}
			}
			if (nPath == null || !nPath.DirectoryExists())
			{
				if (string.IsNullOrEmpty(vcToolsVersion))
				{
					return;
				}
				nPath = installationPath.Combine("VC", "Tools", "MSVC", vcToolsVersion);
				if (!nPath.DirectoryExists())
				{
					string text2 = vcToolsVersion.Substring(0, vcToolsVersion.LastIndexOf('.'));
					nPath = installationPath.Combine("VC", "Tools", "MSVC", text2);
				}
			}
			NPath nPath3 = nPath.Combine("bin", _hostDirectory, architectureFolder);
			NPath nPath4 = nPath.Combine("include");
			NPath nPath5 = nPath.Combine("lib", architectureFolder);
			if (nPath3.DirectoryExists() && nPath4.DirectoryExists() && nPath5.DirectoryExists())
			{
				NPath vCRedistForArchitecture = GetVCRedistForArchitecture(architecture, installationPath, vcToolsVersion);
				vcToolsPaths.Add(architecture.GetType(), new VCPaths(nPath3, nPath4, nPath5, vCRedistForArchitecture));
			}
		}

		private static NPath GetVCRedistForArchitecture(Architecture architecture, NPath installationPath, string vcToolsVersion)
		{
			NPath nPath = null;
			NPath nPath2 = installationPath.Combine("VC", "Auxiliary", "Build", "Microsoft.VCRedistVersion.default.txt");
			if (nPath2.FileExists())
			{
				string text = null;
				try
				{
					text = nPath2.ReadAllText().Trim();
				}
				catch
				{
				}
				if (text != null)
				{
					nPath = installationPath.Combine("VC", "Redist", "MSVC", text);
				}
			}
			if (nPath == null || !nPath.DirectoryExists())
			{
				if (string.IsNullOrEmpty(vcToolsVersion))
				{
					return null;
				}
				nPath = installationPath.Combine("VC", "Redist", "MSVC", vcToolsVersion);
				ConsoleOutput.Always.WriteLine($"2-vsRedistDir {nPath}");
				if (!nPath.DirectoryExists())
				{
					string text2 = vcToolsVersion.Substring(0, vcToolsVersion.LastIndexOf('.'));
					nPath = installationPath.Combine("VC", "Redist", "MSVC", text2);
				}
			}
			if (nPath != null && nPath.DirectoryExists())
			{
				string text3;
				if (architecture is x86Architecture)
				{
					text3 = "x86";
				}
				else if (architecture is x64Architecture)
				{
					text3 = "x64";
				}
				else if (architecture is ARMv7Architecture)
				{
					text3 = "arm";
				}
				else
				{
					if (!(architecture is ARM64Architecture))
					{
						throw new NotSupportedException($"Architecture {architecture} is not supported by MsvcToolChain!");
					}
					text3 = "arm64";
				}
				nPath = nPath.Combine(text3);
			}
			if ((object)nPath == null || !nPath.DirectoryExists())
			{
				return null;
			}
			return nPath;
		}
	}
}
