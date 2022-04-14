using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NiceIO;
using Unity.IL2CPP.Building.ToolChains.Android;
using Unity.IL2CPP.Common;
using Unity.MiniProfiling;

namespace Unity.IL2CPP.Building.ToolChains
{
	public class AndroidToolChain : CppToolChain
	{
		private readonly Func<string, IEnumerable<string>> AdditionalCompilerOptionsForSourceFile;

		private readonly bool _assemblyOutput;

		public AndroidNDKUtilities AndroidNDK { get; }

		public override string DynamicLibraryExtension => ".so";

		public override string StaticLibraryExtension => ".a";

		public override bool SupportsMapFileParser => false;

		public AndroidToolChain(Architecture architecture, BuildConfiguration buildConfiguration, bool treatWarningsAsErrors, bool assemblyOutput, bool useDependenciesToolChain, NPath toolchainPath = null)
			: base(architecture, buildConfiguration)
		{
			AndroidNDK = new AndroidNDKUtilities(toolchainPath, architecture, useDependenciesToolChain);
			if (treatWarningsAsErrors)
			{
				AdditionalCompilerOptionsForSourceFile = FlagsToMakeWarningsErrorsFor;
			}
			_assemblyOutput = assemblyOutput;
		}

		public override string ExecutableExtension()
		{
			return string.Empty;
		}

		public override bool CanBuildInCurrentEnvironment()
		{
			if (!PlatformUtils.IsOSX() && !PlatformUtils.IsWindows())
			{
				return PlatformUtils.IsLinux();
			}
			return true;
		}

		public override IEnumerable<string> ToolChainDefines()
		{
			yield return "LINUX";
			yield return "ANDROID";
			yield return "PLATFORM_ANDROID";
			yield return "__linux__";
			yield return "__STDC_FORMAT_MACROS";
			if (base.Architecture is ARM64Architecture)
			{
				yield return "TARGET_ARM64";
			}
		}

		public override IEnumerable<NPath> ToolChainIncludePaths()
		{
			yield return new NPath("");
		}

		public override IEnumerable<string> OutputArgumentFor(NPath objectFile, NPath sourceFile)
		{
			string outputFileName;
			if (_assemblyOutput)
			{
				yield return "-S";
				outputFileName = sourceFile.ChangeExtension("s").InQuotes(SlashMode.Forward);
			}
			else
			{
				outputFileName = objectFile.InQuotes(SlashMode.Forward);
			}
			yield return "-o";
			yield return outputFileName;
		}

		public override string ObjectExtension()
		{
			return ".o";
		}

		public override IEnumerable<NPath> ToolChainLibraryPaths()
		{
			string text = "arch-";
			if (base.Architecture is ARMv7Architecture)
			{
				text += "arm";
			}
			else
			{
				if (!(base.Architecture is x86Architecture))
				{
					throw new NotSupportedException(string.Format("Architecture {0} is not supported by {1}.", base.Architecture, "AndroidToolChain"));
				}
				text += "x86";
			}
			yield return AndroidNDK.AndroidNdkRootDir.Combine("platforms", "android-19", text, "usr", "lib");
		}

		public override IEnumerable<string> CompilerFlagsFor(CppCompilationInstruction cppCompilationInstruction)
		{
			foreach (string define in cppCompilationInstruction.Defines)
			{
				yield return "-D" + define;
			}
			foreach (NPath includePath in cppCompilationInstruction.IncludePaths)
			{
				yield return "-I" + includePath.InQuotes();
			}
			foreach (NPath stlIncludePath in AndroidNDK.StlIncludePaths)
			{
				yield return "-I" + stlIncludePath.InQuotes();
			}
			foreach (string item in ChooseCompilerFlags(cppCompilationInstruction, DefaultCompilerFlags))
			{
				yield return item;
			}
			yield return cppCompilationInstruction.SourceFile.InQuotes();
		}

		protected override string GetInterestingOutputFromCompilationShellResult(Shell.ExecuteResult shellResult)
		{
			return shellResult.StdErr;
		}

		protected override string GetInterestingOutputFromLinkerShellResult(Shell.ExecuteResult shellResult)
		{
			return shellResult.StdErr;
		}

		public override NPath CompilerExecutableFor(NPath sourceFile)
		{
			if (!sourceFile.HasExtension(".c"))
			{
				return AndroidNDK.CppCompilerPath;
			}
			return AndroidNDK.CCompilerPath;
		}

		public override CppProgramBuilder.LinkerInvocation MakeLinkerInvocation(IEnumerable<NPath> objectFiles, NPath outputFile, IEnumerable<NPath> staticLibraries, IEnumerable<NPath> dynamicLibraries, IEnumerable<string> specifiedLinkerFlags, CppToolChainContext toolChainContext)
		{
			string tempFileName = Path.GetTempFileName();
			File.WriteAllText(tempFileName, objectFiles.InQuotes(SlashMode.Forward).SeparateWithSpaces(), Encoding.ASCII);
			if (BuildShell.LogEnabled)
			{
				foreach (NPath objectFile in objectFiles)
				{
					BuildShell.AppendToCommandLog("ResponseFile: {0}: {1}", tempFileName, objectFile);
				}
			}
			List<string> list = new List<string>
			{
				"@" + tempFileName.InQuotes(),
				"-o",
				outputFile.InQuotes()
			};
			list.AddRange(ChooseLinkerFlags(staticLibraries, dynamicLibraries, outputFile, specifiedLinkerFlags, DefaultLinkerFlags));
			List<NPath> list2 = new List<NPath>(objectFiles);
			list2.AddRange(staticLibraries);
			list2.AddRange(dynamicLibraries);
			return new CppProgramBuilder.LinkerInvocation
			{
				ExecuteArgs = new Shell.ExecuteArgs
				{
					Executable = AndroidNDK.LinkerPath.ToString(),
					Arguments = list.SeparateWithSpaces()
				},
				ArgumentsInfluencingOutcome = list.GetRange(1, list.Count - 1),
				FilesInfluencingOutcome = list2
			};
		}

		public override void OnAfterLink(NPath outputFile, CppToolChainContext toolChainContext, bool forceRebuild, bool verbose)
		{
			NPath nPath = outputFile.Parent.Combine(outputFile.FileNameWithoutExtension);
			bool flag = outputFile.HasExtension(DynamicLibraryExtension);
			NPath nPath2 = new NPath(string.Concat(nPath, flag ? ".dbg.so" : ".dbg"));
			if (nPath2.FileExists())
			{
				nPath2.Delete();
			}
			File.Move(outputFile.ToString(), nPath2.ToString());
			List<string> list = new List<string>();
			list.Add(nPath2.InQuotes());
			list.Add(outputFile.InQuotes());
			list.Add((base.BuildConfiguration == BuildConfiguration.Debug) ? "--strip-debug" : "--strip-all");
			Shell.ExecuteArgs args = new Shell.ExecuteArgs
			{
				Executable = AndroidNDK.ObjCopyPath.ToString(),
				Arguments = list.SeparateWithSpaces()
			};
			using (MiniProfiler.Section("strip"))
			{
				BuildShell.Execute(args);
			}
			NPath nPath3 = new NPath(string.Concat(nPath, flag ? ".sym.so" : ".sym"));
			list = new List<string>();
			list.Add(nPath2.InQuotes());
			list.Add(nPath3.InQuotes());
			list.Add("--only-keep-debug");
			args = new Shell.ExecuteArgs
			{
				Executable = AndroidNDK.ObjCopyPath.ToString(),
				Arguments = list.SeparateWithSpaces()
			};
			using (MiniProfiler.Section("strip", "debug"))
			{
				BuildShell.Execute(args);
			}
			list = new List<string>();
			list.Add("--add-gnu-debuglink=" + nPath3.InQuotes());
			list.Add(outputFile.InQuotes());
			args = new Shell.ExecuteArgs
			{
				Executable = AndroidNDK.ObjCopyPath.ToString(),
				Arguments = list.SeparateWithSpaces()
			};
			using (MiniProfiler.Section("strip", "debug link"))
			{
				BuildShell.Execute(args);
			}
		}

		private IEnumerable<string> DefaultCompilerFlags(CppCompilationInstruction cppCompilationInstruction)
		{
			bool buildCppSource = cppCompilationInstruction.SourceFile.HasExtension(".cpp");
			yield return "-c";
			yield return "-g";
			yield return (base.BuildConfiguration == BuildConfiguration.Debug) ? "-DDEBUG" : "-DNDEBUG";
			yield return "-fexceptions";
			yield return "-fno-limit-debug-info";
			yield return "-fdata-sections";
			yield return "-ffunction-sections";
			yield return "-Wa,--noexecstack";
			yield return "-fno-rtti";
			if (buildCppSource)
			{
				yield return "-std=c++11";
			}
			yield return "-fno-strict-aliasing";
			yield return "-fvisibility=hidden";
			yield return "-fvisibility-inlines-hidden";
			yield return "-fno-strict-overflow";
			if (!AndroidNDK.LegacyStl)
			{
				yield return "-fno-addrsig";
			}
			if (!cppCompilationInstruction.CompilerFlags.Contains("-fPIE"))
			{
				yield return "-fPIC";
			}
			yield return (base.BuildConfiguration == BuildConfiguration.Debug) ? "-O0" : "-Os";
			if (AndroidNDK.GnuBinutils)
			{
				yield return "--sysroot " + AndroidNDK.CompilerSysRoot.InQuotes();
				yield return "-gcc-toolchain " + AndroidNDK.GccToolchain.InQuotes();
			}
			else if (buildCppSource)
			{
				yield return "-stdlib=libc++";
			}
			yield return "-target " + AndroidNDK.Platform;
			if (cppCompilationInstruction.SourceFile.ToString().Contains("krait_signal_handler") && base.Architecture is ARMv7Architecture)
			{
				yield return "-mthumb";
			}
			foreach (string architectureCompilerFlag in AndroidNDK.ArchitectureCompilerFlags)
			{
				yield return architectureCompilerFlag;
			}
			if (AdditionalCompilerOptionsForSourceFile != null)
			{
				foreach (string item in AdditionalCompilerOptionsForSourceFile(cppCompilationInstruction.SourceFile.ToString()))
				{
					yield return item;
				}
			}
			yield return "-Wno-unused-value";
		}

		private IEnumerable<string> DefaultLinkerFlags(IEnumerable<NPath> staticLibraries, IEnumerable<NPath> dynamicLibraries, NPath outputFile)
		{
			if (outputFile.HasExtension(DynamicLibraryExtension))
			{
				yield return "-shared";
				yield return "-Wl,-soname," + outputFile.FileName;
			}
			else
			{
				yield return "-fPIE";
				yield return "-pie";
			}
			yield return "-Wl,--no-undefined";
			yield return "-Wl,-z,noexecstack";
			yield return "-Wl,--gc-sections";
			yield return "-Wl,--build-id";
			if (AndroidNDK.GnuBinutils)
			{
				yield return "--sysroot " + AndroidNDK.LinkerSysRoot.InQuotes();
				yield return "-gcc-toolchain " + AndroidNDK.GccToolchain.InQuotes();
				if (!AndroidNDK.LegacyStl)
				{
					yield return "-nostdlib++";
				}
			}
			else
			{
				yield return "-stdlib=libc++";
				yield return "-static-libstdc++";
			}
			yield return "-target " + AndroidNDK.Platform;
			yield return "-Wl,--wrap,sigaction";
			foreach (NPath stlLibraryPath in AndroidNDK.StlLibraryPaths)
			{
				yield return "-L " + stlLibraryPath.InQuotes();
			}
			foreach (NPath staticLibrary in staticLibraries)
			{
				yield return staticLibrary.InQuotes();
			}
			foreach (NPath dynamicLibrary in dynamicLibraries)
			{
				yield return "-l " + dynamicLibrary.InQuotes();
			}
			foreach (string stlLibrary in AndroidNDK.StlLibraries)
			{
				yield return "-l" + stlLibrary;
			}
			yield return "-llog";
			yield return "-rdynamic";
			if (base.BuildConfiguration == BuildConfiguration.ReleasePlus && AndroidNDK.CanUseGoldLinker(base.BuildConfiguration))
			{
				yield return "-Wl,--icf=safe";
				yield return "-Wl,--icf-iterations=5";
			}
			foreach (string architectureLinkerFlag in AndroidNDK.GetArchitectureLinkerFlags(base.BuildConfiguration))
			{
				yield return architectureLinkerFlag;
			}
		}

		private static IEnumerable<string> FlagsToMakeWarningsErrorsFor(string sourceFile)
		{
			if (!sourceFile.Contains("pinvoke-targets.cpp") && !sourceFile.Replace('\\', '/').Contains("external/mono/mono"))
			{
				yield return "-Werror";
				yield return "-Wno-trigraphs";
				yield return "-Wno-tautological-compare";
				yield return "-Wno-invalid-offsetof";
				yield return "-Wno-implicitly-unsigned-literal";
				yield return "-Wno-integer-overflow";
				yield return "-Wno-shift-negative-value";
				yield return "-Wno-unknown-attributes";
				yield return "-Wno-implicit-function-declaration";
				yield return "-Wno-null-conversion";
				yield return "-Wno-missing-declarations";
			}
		}

		public override NPath GetLibraryFileName(NPath library)
		{
			if (library.Depth != 1)
			{
				throw new ArgumentException($"Invalid library '{library}'.", "library");
			}
			return new NPath("lib" + library.FileNameWithoutExtension + ".so");
		}

		public override bool CanGenerateAssemblyCode()
		{
			return true;
		}

		public override SourceCodeSearcher SourceCodeSearcher()
		{
			return new AndroidToolChainAssemblyCodeSearcher();
		}
	}
}
