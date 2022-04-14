using System;
using Microsoft.Win32;
using NiceIO;

namespace Unity.IL2CPP.Building.ToolChains.MsvcVersions
{
	public static class WindowsSDKs
	{
		public static NPath GetWindows7SDKDirectory()
		{
			RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Microsoft SDKs\\Windows\\v7.0A");
			if (registryKey == null)
			{
				return null;
			}
			string text = (string)registryKey.GetValue("InstallationFolder");
			if (string.IsNullOrEmpty(text))
			{
				return null;
			}
			return text.ToNPath();
		}

		public static NPath GetWindows81SDKDirectory()
		{
			RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Microsoft SDKs\\Windows\\v8.1");
			if (registryKey == null)
			{
				return null;
			}
			string text = (string)registryKey.GetValue("InstallationFolder");
			if (string.IsNullOrEmpty(text))
			{
				return null;
			}
			return text.ToNPath();
		}

		internal static Tuple<NPath, string> GetWindows10SDKDirectoryAndVersion()
		{
			string version;
			return new Tuple<NPath, string>(GetWindows10SDKDirectory(out version), version);
		}

		public static NPath GetWindows10SDKDirectory(out string version)
		{
			version = null;
			RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Microsoft SDKs\\Windows\\v10.0");
			if (registryKey == null)
			{
				return null;
			}
			string text = (string)registryKey.GetValue("InstallationFolder");
			if (string.IsNullOrEmpty(text))
			{
				return null;
			}
			string text2 = (string)registryKey.GetValue("ProductVersion");
			Version version2 = ((!string.IsNullOrEmpty(text2)) ? Version.Parse(text2) : new Version(10, 0, 10240));
			if (version2.Build == -1)
			{
				version2 = new Version(version2.Major, version2.Minor, 0, 0);
			}
			else if (version2.Revision == -1)
			{
				version2 = new Version(version2.Major, version2.Minor, version2.Build, 0);
			}
			version = version2.ToString();
			return text.ToNPath();
		}

		public static NPath GetDotNetFrameworkSDKDirectory()
		{
			RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\WOW6432Node\\Microsoft\\Microsoft SDKs\\NETFXSDK\\4.6.1");
			if (registryKey == null)
			{
				registryKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\WOW6432Node\\Microsoft\\Microsoft SDKs\\NETFXSDK\\4.6");
			}
			if (registryKey == null)
			{
				return null;
			}
			string text = (string)registryKey.GetValue("KitsInstallationFolder");
			if (string.IsNullOrEmpty(text))
			{
				return null;
			}
			return text.ToNPath();
		}

		public static NPath GetDotNetFrameworkToolsDirectory()
		{
			RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\WOW6432Node\\Microsoft\\Microsoft SDKs\\NETFXSDK\\4.6.1");
			if (registryKey == null)
			{
				registryKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\WOW6432Node\\Microsoft\\Microsoft SDKs\\NETFXSDK\\4.6");
			}
			if (registryKey == null)
			{
				return null;
			}
			string text = (string)registryKey.GetValue("InstallationFolder");
			if (string.IsNullOrEmpty(text))
			{
				return null;
			}
			NPath nPath = text.ToNPath().Combine("bin");
			string[] array = new string[5] { "NETFX 4.7.2 Tools", "NETFX 4.7.1 Tools", "NETFX 4.7 Tools", "NETFX 4.6.1 Tools", "NETFX 4.6 Tools" };
			foreach (string text2 in array)
			{
				NPath nPath2 = nPath.Combine(text2);
				if (nPath2.DirectoryExists())
				{
					return nPath2;
				}
			}
			return null;
		}
	}
}
