using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NiceIO;
using Unity.IL2CPP.Building.ToolChains.MsvcVersions;
using Unity.IL2CPP.Common;

namespace Unity.IL2CPP.Building.ToolChains
{
	public class MsvcWindowsGamesToolChain : MsvcToolChain
	{
		private readonly bool _useDependenciesToolChain;

		private MsvcSystemInstallationBase _msvcInstallation;

		private Version _minSdkVersion;

		private NPath _netfxsdkDir;

		public override MsvcInstallation MsvcInstallation
		{
			get
			{
				if (_msvcInstallation == null)
				{
					if (_useDependenciesToolChain)
					{
						_msvcInstallation = MsvcInstallation.GetWindowsGamesDependenciesInstallation() as MsvcSystemInstallationBase;
					}
					else
					{
						_msvcInstallation = MsvcInstallation.GetLatestFunctionalInstallationAtLeast(new Version(15, 0), base.Architecture) as MsvcSystemInstallationBase;
					}
				}
				return _msvcInstallation;
			}
		}

		public MsvcWindowsGamesToolChain(Architecture architecture, BuildConfiguration buildConfiguration, bool treatWarningsAsErrors, bool assemblyOutput, bool useDependenciesToolChain, NPath toolchainPath, bool disableExceptions = false, string showIncludes = "")
			: base(architecture, buildConfiguration, treatWarningsAsErrors, assemblyOutput, disableExceptions, showIncludes)
		{
			_minSdkVersion = Version.Parse(toolchainPath.ToString());
			_netfxsdkDir = WindowsSDKs.GetDotNetFrameworkSDKDirectory();
			_useDependenciesToolChain = useDependenciesToolChain;
		}

		protected override void PostProcessToolChainContext(MsvcToolChainContext context)
		{
			context.UseDependenciesToolChain = _useDependenciesToolChain;
		}

		public override IEnumerable<string> ToolChainDefines()
		{
			foreach (string item in base.ToolChainDefines())
			{
				yield return item;
			}
			if (base.DontLinkCrt)
			{
				yield return "IL2CPP_NO_CRT";
			}
			yield return "WINAPI_FAMILY=WINAPI_FAMILY_GAMES";
			yield return "DONT_USE_USER32_DLL";
			yield return "NO_HAVE_TRANSMIT_FILE_BUFFERS";
		}

		public override IEnumerable<string> ToolChainStaticLibraries()
		{
			foreach (string item in base.ToolChainStaticLibraries())
			{
				yield return item;
			}
			yield return "kernel32.lib";
			yield return "advapi32.lib";
			yield return "Crypt32.lib";
			yield return "ws2_32.lib";
			yield return "Ole32.lib";
			yield return "Iphlpapi.lib";
		}

		protected override IEnumerable<string> DefaultCompilerFlags(CppCompilationInstruction cppCompilationInstruction)
		{
			foreach (string item in base.DefaultCompilerFlags(cppCompilationInstruction))
			{
				yield return item;
			}
			yield return (base.BuildConfiguration == BuildConfiguration.Debug) ? "/MDd" : "/MD";
		}

		protected override IEnumerable<string> GetDefaultLinkerArgs(IEnumerable<NPath> staticLibraries, IEnumerable<NPath> dynamicLibraries, NPath outputFile)
		{
			foreach (string defaultLinkerArg in base.GetDefaultLinkerArgs(staticLibraries, dynamicLibraries, outputFile))
			{
				yield return defaultLinkerArg;
			}
			if (outputFile.HasExtension(".exe"))
			{
				yield return "/SUBSYSTEM:CONSOLE";
				if (!base.DontLinkCrt)
				{
					yield return "/ENTRY:wWinMainCRTStartup";
				}
			}
			else
			{
				yield return "/SUBSYSTEM:WINDOWS";
			}
		}

		public override void OnAfterLink(NPath outputFile, CppToolChainContext toolChainContext, bool forceRebuild, bool verbose)
		{
			foreach (NPath item in _msvcInstallation.GetVcRedistPath(base.BuildConfiguration, base.Architecture, "").Directories("Microsoft.*CRT").First()
				.Files())
			{
				item.Copy(outputFile.Parent);
			}
			if (base.BuildConfiguration == BuildConfiguration.Debug)
			{
				_msvcInstallation.GetSDKBinDirectoryFor(new x64Architecture(), "ucrt/ucrtbased.dll")?.Copy(outputFile.Parent);
			}
		}

		public override bool CanBuildInCurrentEnvironment()
		{
			try
			{
				return base.CanBuildInCurrentEnvironment() && MsvcInstallation.WindowsSDKBuildVersion >= _minSdkVersion.Build;
			}
			catch
			{
				return false;
			}
		}

		public override string GetCannotBuildInCurrentEnvironmentErrorMessage()
		{
			if (CanBuildInCurrentEnvironment())
			{
				return null;
			}
			if (!_useDependenciesToolChain)
			{
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.AppendLine("C++ code builder is unable to build C++ code. In order to build C++ code for Windows Games, you must have one of these installed:");
				try
				{
					stringBuilder.AppendLine("    " + MsvcInstallation.GetMsvcVersionRequirementsForBuildingAndReasonItCannotBuild(new Version(15, 0), base.Architecture));
				}
				catch (InvalidOperationException)
				{
					if (MsvcInstallation.WindowsSDKBuildVersion < _minSdkVersion.Build)
					{
						stringBuilder.AppendLine($"    Visual Studio 2017 with C++ compilers and Windows 10 SDK (it cannot build because Windows SDK Version >= {_minSdkVersion} required)");
						stringBuilder.AppendLine("        Visual Studio 2017 installation is found using Microsoft.VisualStudio.Setup.Configuration COM APIs");
						stringBuilder.AppendLine("        Windows 10 SDK is found by looking at \"SOFTWARE\\Wow6432Node\\Microsoft\\Microsoft SDKs\\Windows\\v10.0\\InstallationFolder\" in the registry");
					}
				}
				return stringBuilder.ToString();
			}
			string text;
			try
			{
				text = MsvcInstallation.GetDependenciesInstallation().GetReasonMsvcInstallationCannotBuild(base.Architecture);
			}
			catch (Exception ex2)
			{
				text = ex2.Message;
			}
			return "Msvc Dependencies ToolChain is unable to build C++ code: " + text;
		}
	}
}
