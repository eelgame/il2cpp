using System;
using System.Diagnostics;
using System.IO;
using NiceIO;
using Unity.IL2CPP.Common;

namespace Unity.IL2CPP.Building.ToolChains
{
	public static class XcodeInstallation
	{
		public static NPath SupportedPath { get; private set; }

		public static bool Exists => SupportedPath.DirectoryExists();

		public static NPath PlatformSupportedPath { get; private set; }

		public static NPath InstalledSdk { get; private set; }

		public static NPath SDKPath => InstalledSdk;

		public static void GetXcodeInfo(string Command, string Args, ref string output)
		{
			ProcessStartInfo processStartInfo = new ProcessStartInfo(Command, Args);
			processStartInfo.UseShellExecute = false;
			processStartInfo.RedirectStandardOutput = true;
			processStartInfo.CreateNoWindow = true;
			try
			{
				using (Process process = Process.Start(processStartInfo))
				{
					StreamReader standardOutput = process.StandardOutput;
					output = standardOutput.ReadToEnd().Trim();
				}
			}
			catch (Exception ex)
			{
				ConsoleOutput.Error.WriteLine(ex.Message);
			}
		}

		static XcodeInstallation()
		{
			string output = "/Applications/Xcode.app";
			string output2 = string.Empty;
			GetXcodeInfo("xcode-select", "-p", ref output);
			SupportedPath = output;
			if (Exists)
			{
				GetXcodeInfo("xcrun", "--sdk macosx --show-sdk-platform-path", ref output2);
				PlatformSupportedPath = output2;
				string output3 = null;
				GetXcodeInfo("xcrun", "--sdk macosx --show-sdk-path", ref output3);
				InstalledSdk = (string.IsNullOrEmpty(output3) ? null : output3);
			}
		}
	}
}
