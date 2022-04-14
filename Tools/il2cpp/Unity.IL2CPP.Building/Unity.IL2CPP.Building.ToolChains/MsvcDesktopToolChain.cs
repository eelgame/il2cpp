using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NiceIO;
using Unity.IL2CPP.Building.ToolChains.MsvcVersions;
using Unity.IL2CPP.Common;

namespace Unity.IL2CPP.Building.ToolChains
{
	public class MsvcDesktopToolChain : MsvcToolChain
	{
		private readonly bool _useDependenciesToolChain;

		private MsvcInstallation _msvcInstallation;

		public override MsvcInstallation MsvcInstallation
		{
			get
			{
				if (_msvcInstallation == null)
				{
					if (_useDependenciesToolChain)
					{
						_msvcInstallation = MsvcInstallation.GetDependenciesInstallation();
					}
					else
					{
						_msvcInstallation = MsvcInstallation.GetLatestFunctionalInstallation(base.Architecture);
					}
				}
				return _msvcInstallation;
			}
		}

		public MsvcDesktopToolChain(Architecture architecture, BuildConfiguration buildConfiguration, bool treatWarningsAsErrors, bool assemblyOutput, bool useDependenciesToolChain, bool disableExceptions = false, string showIncludes = "")
			: base(architecture, buildConfiguration, treatWarningsAsErrors, assemblyOutput, disableExceptions, showIncludes)
		{
			_useDependenciesToolChain = useDependenciesToolChain;
		}

		public MsvcDesktopToolChain(MsvcInstallation msvcInstallation, Architecture architecture, BuildConfiguration buildConfiguration, bool treatWarningsAsErrors, bool assemblyOutput)
			: base(architecture, buildConfiguration, treatWarningsAsErrors, assemblyOutput)
		{
			_msvcInstallation = msvcInstallation;
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
			yield return "WINAPI_FAMILY=WINAPI_FAMILY_DESKTOP_APP";
		}

		public override IEnumerable<string> ToolChainStaticLibraries()
		{
			foreach (string item in base.ToolChainStaticLibraries())
			{
				yield return item;
			}
			yield return "kernel32.lib";
			yield return "user32.lib";
			yield return "advapi32.lib";
			yield return "ole32.lib";
			yield return "oleaut32.lib";
			yield return "Shell32.lib";
			yield return "Crypt32.lib";
			yield return "psapi.lib";
			yield return "version.lib";
			yield return "MsWSock.lib";
			yield return "ws2_32.lib";
			yield return "Iphlpapi.lib";
			yield return "Dbghelp.lib";
		}

		protected override IEnumerable<string> DefaultCompilerFlags(CppCompilationInstruction cppCompilationInstruction)
		{
			foreach (string item in base.DefaultCompilerFlags(cppCompilationInstruction))
			{
				yield return item;
			}
			bool num = cppCompilationInstruction.CompilerFlags.Any((string flag) => flag.ToLower().StartsWith("/clr"));
			bool flag2 = cppCompilationInstruction.CompilerFlags.Any((string flag) => flag.ToLower().StartsWith("/zw"));
			if (!num && !flag2)
			{
				yield return (base.BuildConfiguration == BuildConfiguration.Debug) ? "/MTd" : "/MT";
			}
			else
			{
				yield return (base.BuildConfiguration == BuildConfiguration.Debug) ? "/MDd" : "/MD";
			}
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

		public override string GetCannotBuildInCurrentEnvironmentErrorMessage()
		{
			if (CanBuildInCurrentEnvironment())
			{
				return null;
			}
			if (!_useDependenciesToolChain)
			{
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.AppendLine("C++ code builder is unable to build C++ code. In order to build C++ code for Windows Desktop, you must have one of these installed:");
				stringBuilder.AppendLine("    " + MsvcInstallation.GetMsvcVersionRequirementsForBuildingAndReasonItCannotBuild(new Version(14, 0), base.Architecture));
				stringBuilder.AppendLine("    " + MsvcInstallation.GetMsvcVersionRequirementsForBuildingAndReasonItCannotBuild(new Version(15, 0), base.Architecture));
				return stringBuilder.ToString();
			}
			string text;
			try
			{
				text = MsvcInstallation.GetDependenciesInstallation().GetReasonMsvcInstallationCannotBuild(base.Architecture);
			}
			catch (Exception ex)
			{
				text = ex.Message;
			}
			return "Msvc Dependencies ToolChain is unable to build C++ code: " + text;
		}
	}
}
