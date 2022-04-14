using System;
using System.Collections.Generic;
using System.Text;
using NiceIO;
using Unity.IL2CPP.Building.ToolChains.MsvcVersions;
using Unity.IL2CPP.Common;

namespace Unity.IL2CPP.Building.ToolChains
{
	public class MsvcWinRtToolChain : MsvcToolChain
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
						_msvcInstallation = MsvcInstallation.GetLatestFunctionalInstallationAtLeast(new Version(14, 0), base.Architecture);
					}
				}
				return _msvcInstallation;
			}
		}

		public MsvcWinRtToolChain(Architecture architecture, BuildConfiguration buildConfiguration, bool treatWarningsAsErrors, bool assemblyOutput, bool useDependenciesToolChain)
			: base(architecture, buildConfiguration, treatWarningsAsErrors, assemblyOutput)
		{
			_useDependenciesToolChain = useDependenciesToolChain;
		}

		public MsvcWinRtToolChain(MsvcInstallation msvcInstallation, Architecture architecture, BuildConfiguration buildConfiguration, bool treatWarningsAsErrors, bool assemblyOutput)
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
			yield return "__WRL_NO_DEFAULT_LIB__";
			yield return "WINAPI_FAMILY=WINAPI_FAMILY_APP";
		}

		public override IEnumerable<string> ToolChainStaticLibraries()
		{
			foreach (string item in base.ToolChainStaticLibraries())
			{
				yield return item;
			}
			yield return "Shcore.lib";
			yield return "WindowsApp.lib";
			yield return "Crypt32.lib";
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
			yield return "/APPCONTAINER";
			yield return "/SUBSYSTEM:WINDOWS";
			yield return "/NODEFAULTLIB:ole32.lib";
			yield return "/NODEFAULTLIB:kernel32.lib";
			if (base.BuildConfiguration == BuildConfiguration.Debug)
			{
				yield return "/NODEFAULTLIB:msvcrt.lib";
			}
		}

		public override IEnumerable<NPath> ToolChainIncludePaths()
		{
			foreach (NPath item in base.ToolChainIncludePaths())
			{
				yield return item;
			}
			NPath nPath = CommonPaths.Il2CppRoot.Combine("Unity.IL2CPP.WinRT");
			if (nPath.DirectoryExists())
			{
				yield return nPath;
			}
		}

		public override IEnumerable<NPath> ToolChainLibraryPaths()
		{
			return MsvcInstallation.GetLibDirectories(base.Architecture, "store");
		}

		public override void OnAfterLink(NPath outputFile, CppToolChainContext toolChainContext, bool forceRebuild, bool verbose)
		{
			if (outputFile.HasExtension(".exe"))
			{
				WinRTManifest.Write(outputFile.Parent, outputFile.FileName, base.Architecture);
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
				stringBuilder.AppendLine("C++ code builder is unable to build C++ code. In order to build C++ code for Universal Windows Platform, you must have one of these installed:");
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
