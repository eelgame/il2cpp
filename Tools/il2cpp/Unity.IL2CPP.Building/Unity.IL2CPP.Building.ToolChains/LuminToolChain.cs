using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NiceIO;
using Unity.IL2CPP.Common;

namespace Unity.IL2CPP.Building.ToolChains
{
	public class LuminToolChain : CppToolChain
	{
		public override string DynamicLibraryExtension => ".so";

		public override string StaticLibraryExtension => ".a";

		public LuminToolChain(Architecture architecture, BuildConfiguration buildConfiguration, bool treatWarningsAsErrorsInGeneratedCode, bool assemblyOutput, bool useDependenciesToolchain, NPath toolchainPath = null)
			: base(architecture, buildConfiguration)
		{
			LuminSDK.InitializeSDK(useDependenciesToolchain ? toolchainPath : null);
			LuminSDK.EnsureExists();
			if (!(architecture is ARM64Architecture))
			{
				throw new ArgumentException("ML architecture must be ARM64");
			}
		}

		public override IEnumerable<string> ToolChainDefines()
		{
			foreach (string define in LuminSDK.Defines)
			{
				yield return define;
			}
			if (base.BuildConfiguration == BuildConfiguration.Debug)
			{
				yield return "IL2CPP_DEBUG";
			}
			if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("IL2CPP_TEST_RUN_NAME")))
			{
				yield return "IL2CPP_TARGET_LUMIN_AUTOMATION";
			}
		}

		public override IEnumerable<string> ToolChainStaticLibraries()
		{
			return LuminSDK.SystemStaticLibraries;
		}

		public IEnumerable<string> ToolChainDynamicLibraries()
		{
			return LuminSDK.SystemDynamicLibraries;
		}

		public override IEnumerable<NPath> ToolChainIncludePaths()
		{
			return LuminSDK.SystemIncludes;
		}

		public override bool CanBuildInCurrentEnvironment()
		{
			if (!PlatformUtils.IsWindows())
			{
				return PlatformUtils.IsOSX();
			}
			return true;
		}

		public override NPath CompilerExecutableFor(NPath sourceFile)
		{
			return LuminSDK.CompilerPath;
		}

		private IEnumerable<string> DefaultCompilerFlags(CppCompilationInstruction cppCompilationInstruction)
		{
			return LuminSDK.CompilerFlags;
		}

		public override IEnumerable<string> CompilerFlagsFor(CppCompilationInstruction cppCompilationInstruction)
		{
			foreach (string define in cppCompilationInstruction.Defines)
			{
				yield return "-D" + define;
			}
			foreach (NPath includePath in cppCompilationInstruction.IncludePaths)
			{
				yield return "-I" + includePath.InQuotes(SlashMode.Forward);
			}
			foreach (string item in ChooseCompilerFlags(cppCompilationInstruction, DefaultCompilerFlags))
			{
				yield return item;
			}
			yield return (base.BuildConfiguration == BuildConfiguration.Debug) ? "-O0" : "-O2";
			if (base.BuildConfiguration == BuildConfiguration.Debug)
			{
				yield return "-g";
			}
			if (cppCompilationInstruction.SourceFile.ExtensionWithDot == ".c")
			{
				yield return "-x c";
			}
			else
			{
				yield return "-x c++";
				yield return "-std=c++11";
			}
			yield return cppCompilationInstruction.SourceFile.InQuotes();
		}

		public override string ObjectExtension()
		{
			return ".o";
		}

		public override string ExecutableExtension()
		{
			return ".elf";
		}

		private IEnumerable<string> DefaultLinkerFlags(IEnumerable<NPath> staticLibraries, IEnumerable<NPath> dynamicLibaries, NPath outputFile)
		{
			IEnumerable<string> enumerable = Enumerable.Empty<string>();
			if (outputFile.ExtensionWithDot == DynamicLibraryExtension)
			{
				enumerable = enumerable.Concat(new string[2] { "-fPIE", "-shared" });
			}
			else if (outputFile.ExtensionWithDot == ExecutableExtension())
			{
				enumerable = enumerable.Append("-Wl,-pie");
			}
			return enumerable.Concat(LuminSDK.LinkerFlags);
		}

		public override CppProgramBuilder.LinkerInvocation MakeLinkerInvocation(IEnumerable<NPath> objectFiles, NPath outputFile, IEnumerable<NPath> staticLibraries, IEnumerable<NPath> dynamicLibraries, IEnumerable<string> specifiedLinkerFlags, CppToolChainContext toolChainContext)
		{
			NPath[] array = objectFiles.ToArray();
			NPath[] array2 = staticLibraries.ToArray();
			NPath[] array3 = dynamicLibraries.ToArray();
			NPath nPath = new NPath(Path.GetTempFileName());
			string contents = array.InQuotes(SlashMode.Forward).SeparateWithSpaces();
			File.WriteAllText(nPath, contents);
			if (BuildShell.LogEnabled)
			{
				NPath[] array4 = array;
				foreach (NPath nPath2 in array4)
				{
					BuildShell.AppendToCommandLog("ResponseFile: {0}: {1}", nPath, nPath2);
				}
			}
			List<string> list = new List<string>();
			list.Add("@" + nPath.InQuotes());
			list.Add("-o " + outputFile.InQuotes(SlashMode.Forward));
			list.AddRange(ChooseLinkerFlags(array2, array3, outputFile, specifiedLinkerFlags, DefaultLinkerFlags));
			list.AddRange(ToolChainLibraryPaths().InQuotes(SlashMode.Forward).PrefixedWith("-L"));
			list.Add("-Wl,-soname," + outputFile.FileName);
			list.AddRange(array2.InQuotes());
			list.AddRange(ToolChainStaticLibraries().PrefixedWith("-l"));
			list.AddRange(ToolChainDynamicLibraries().PrefixedWith("-l"));
			List<NPath> list2 = new List<NPath>(array);
			list2.AddRange(array2);
			list2.AddRange(array3);
			return new CppProgramBuilder.LinkerInvocation
			{
				ExecuteArgs = new Shell.ExecuteArgs
				{
					Executable = LuminSDK.LinkerPath,
					Arguments = list.SeparateWithSpaces()
				},
				ArgumentsInfluencingOutcome = list.GetRange(1, list.Count - 1),
				FilesInfluencingOutcome = list2
			};
		}

		public override IEnumerable<string> OutputArgumentFor(NPath objectFile, NPath sourceFile)
		{
			return new string[2]
			{
				"-o",
				objectFile.InQuotes()
			};
		}

		protected override string GetInterestingOutputFromCompilationShellResult(Shell.ExecuteResult shellResult)
		{
			return shellResult.StdOut + shellResult.StdErr;
		}

		protected override string GetInterestingOutputFromLinkerShellResult(Shell.ExecuteResult shellResult)
		{
			if (!string.IsNullOrEmpty(shellResult.StdOut))
			{
				ConsoleOutput.Info.WriteLine("linker stdout :" + shellResult.StdOut);
			}
			if (!string.IsNullOrEmpty(shellResult.StdErr))
			{
				ConsoleOutput.Info.WriteLine("linker stderr :" + shellResult.StdErr);
			}
			return shellResult.StdOut + shellResult.StdErr;
		}

		public override IEnumerable<NPath> ToolChainLibraryPaths()
		{
			return LuminSDK.LibraryPaths;
		}
	}
}
