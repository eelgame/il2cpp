using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using NiceIO;
using Unity.IL2CPP.Common;
using Unity.Options;

namespace Unity.IL2CPP.Building.ToolChains
{
	public class EmscriptenToolChain : CppToolChain
	{
		[ProgramOptions]
		public class EmscriptenBuildingOptions
		{
			[HideFromHelp]
			public static string[] JsPre;

			[HideFromHelp]
			public static string[] JsLibraries;

			[HideFromHelp]
			public static string EmscriptenTemp;

			[HideFromHelp]
			public static string EmscriptenCache;

			public static void SetToDefaults()
			{
				JsPre = null;
				JsLibraries = null;
				EmscriptenTemp = null;
				EmscriptenCache = null;
			}
		}

		public static bool DeepDebugging;

		private static bool TargetingWasm;

		private readonly bool _setEnvironmentVariables;

		private readonly bool _useDependenciesToolChain;

		private readonly bool _disableExceptions;

		private readonly bool _enableScriptDebugging;

		private readonly NPath _dataFolder;

		private readonly bool _targetingThreads;

		private readonly bool _runAppInThread;

		public override string DynamicLibraryExtension
		{
			get
			{
				throw new InvalidOperationException("Emscripten does not support dynamic libraries");
			}
		}

		public override string StaticLibraryExtension => ".bc";

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetShortPathName", SetLastError = true)]
		private static extern int WindowsGetShortPathName([MarshalAs(UnmanagedType.LPWStr)] string lpszLongPath, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder lpszShortPath, int cchBuffer);

		private static string GetShortPathName(string path)
		{
			if (!PlatformUtils.IsWindows() || Encoding.UTF8.GetByteCount(path) == path.Length)
			{
				return path;
			}
			int num = WindowsGetShortPathName(path, null, 0);
			if (num == 0)
			{
				return path;
			}
			StringBuilder stringBuilder = new StringBuilder(num);
			num = WindowsGetShortPathName(path, stringBuilder, stringBuilder.Capacity);
			if (num == 0)
			{
				return path;
			}
			return stringBuilder.ToString(0, num);
		}

		public EmscriptenToolChain(Unity.IL2CPP.Common.Architecture architecture, BuildConfiguration buildConfiguration)
			: this(architecture, buildConfiguration, setEnvironmentVariables: false, EmscriptenPaths.UseDependenciesToolChain)
		{
		}

		public EmscriptenToolChain(Unity.IL2CPP.Common.Architecture architecture, BuildConfiguration buildConfiguration, bool setEnvironmentVariables = false, bool useDependenciesToolChain = false, bool disableExceptions = false, bool enableScriptDebugging = false, NPath dataFolder = null)
			: base(architecture, buildConfiguration)
		{
			_setEnvironmentVariables = setEnvironmentVariables;
			_useDependenciesToolChain = useDependenciesToolChain;
			EmscriptenPaths.UseDependenciesToolChain = useDependenciesToolChain;
			_disableExceptions = disableExceptions;
			_enableScriptDebugging = enableScriptDebugging;
			if (_enableScriptDebugging)
			{
				if (dataFolder == null)
				{
					throw new InvalidOperationException("The data directory must be specified in order to build with Emscripten for the managed code debugger.");
				}
				_dataFolder = dataFolder;
				_targetingThreads = true;
				_runAppInThread = true;
			}
		}

		public override IEnumerable<NPath> ToolChainIncludePaths()
		{
			yield break;
		}

		public override IEnumerable<string> ToolChainDefines()
		{
			if (base.BuildConfiguration != 0)
			{
				yield return "NDEBUG";
			}
		}

		public override IEnumerable<string> OutputArgumentFor(NPath objectFile, NPath sourceFile)
		{
			return new string[2]
			{
				"-o",
				objectFile.InQuotes()
			};
		}

		public override string ObjectExtension()
		{
			return ".o";
		}

		public override string ExecutableExtension()
		{
			return ".html";
		}

		public override bool CanBuildInCurrentEnvironment()
		{
			return true;
		}

		public override Dictionary<string, string> EnvVars()
		{
			if (_setEnvironmentVariables)
			{
				return EmscriptenPaths.EmscriptenEnvironmentVariables;
			}
			return null;
		}

		public override IEnumerable<Type> SupportedArchitectures()
		{
			return new Type[1] { typeof(EmscriptenJavaScriptArchitecture) };
		}

		protected override string GetInterestingOutputFromCompilationShellResult(Shell.ExecuteResult shellResult)
		{
			return shellResult.StdErr;
		}

		protected override string GetInterestingOutputFromLinkerShellResult(Shell.ExecuteResult shellResult)
		{
			return shellResult.StdErr;
		}

		public override CppProgramBuilder.LinkerInvocation MakeLinkerInvocation(IEnumerable<NPath> objectFiles, NPath outputFile, IEnumerable<NPath> staticLibraries, IEnumerable<NPath> dynamicLibraries, IEnumerable<string> specifiedLinkerFlags, CppToolChainContext toolChainContext)
		{
			List<NPath> list = new List<NPath>(objectFiles);
			string shortPathName = GetShortPathName(Path.GetFullPath(NPath.CreateTempDirectory("").CreateFile("response.rsp").ToString()));
			NPath nPath = outputFile ?? NPath.CreateTempDirectory("em_out").Combine("test.html");
			List<string> list2 = new List<string>
			{
				"-E",
				EmscriptenPaths.Emcc.InQuotes(),
				"-o",
				nPath.InQuotes(),
				"-s",
				"TOTAL_MEMORY=384MB"
			};
			if (_enableScriptDebugging)
			{
				list2.Add("--preload-file");
				list2.Add($"\"{_dataFolder}@\"");
			}
			if (!_disableExceptions)
			{
				list2.Add("-s");
				list2.Add("DISABLE_EXCEPTION_CATCHING=0");
			}
			if (_targetingThreads)
			{
				list2.Add("-s");
				list2.Add("USE_PTHREADS=1");
				list2.Add("--threadprofiler");
				if (_runAppInThread)
				{
					list2.Add("-s");
					list2.Add("PROXY_TO_PTHREAD=1");
					list2.Add("-lwebsocket.js");
					list2.Add("-s");
					list2.Add("PROXY_POSIX_SOCKETS=1");
					if (base.BuildConfiguration == BuildConfiguration.Debug)
					{
						list2.Add("-s");
						list2.Add("WEBSOCKET_DEBUG=1");
					}
				}
			}
			if (TargetingWasm)
			{
				list2.Add("-s");
				list2.Add("WASM=1");
			}
			else
			{
				list2.Add("-s");
				list2.Add("WASM=0");
			}
			if (_useDependenciesToolChain)
			{
				list2.Add("-s");
				list2.Add("NO_EXIT_RUNTIME=0");
			}
			list2.AddRange(ChooseLinkerFlags(staticLibraries, dynamicLibraries, outputFile, specifiedLinkerFlags, DefaultLinkerFlags));
			string text = (objectFiles.Any() ? objectFiles.First().Parent.ToString() : null);
			if (BuildShell.LogEnabled)
			{
				foreach (NPath objectFile in objectFiles)
				{
					if (text != null)
					{
						BuildShell.AppendToCommandLog("ResponseFile: {0}: {1}", shortPathName, objectFile.RelativeTo(new NPath(text)));
					}
					else
					{
						BuildShell.AppendToCommandLog("ResponseFile: {0}: {1}", shortPathName, objectFile.FileName);
					}
				}
			}
			using (TextWriter textWriter = new StreamWriter(shortPathName))
			{
				foreach (NPath objectFile2 in objectFiles)
				{
					if (text != null)
					{
						textWriter.Write("\"{0}\"\n", objectFile2.RelativeTo(new NPath(text)));
					}
					else
					{
						textWriter.Write("\"{0}\"\n", objectFile2.FileName);
					}
				}
			}
			ConsoleOutput.Info.WriteLine("Response file: {0}", shortPathName);
			list.AddRange(staticLibraries);
			if (EmscriptenBuildingOptions.JsPre != null)
			{
				string[] jsPre = EmscriptenBuildingOptions.JsPre;
				foreach (string text2 in jsPre)
				{
					list2.Add("--pre-js \"" + text2 + "\"");
				}
				list.AddRange(EmscriptenBuildingOptions.JsPre.Select((string path) => new NPath(path)));
			}
			if (EmscriptenBuildingOptions.JsLibraries != null)
			{
				string[] jsPre = EmscriptenBuildingOptions.JsLibraries;
				foreach (string text3 in jsPre)
				{
					list2.Add("--js-library \"" + text3 + "\"");
				}
				list.AddRange(EmscriptenBuildingOptions.JsLibraries.Select((string path) => new NPath(path)));
			}
			list2.AddRange(staticLibraries.InQuotes());
			list2.AddRange(ToolChainStaticLibraries().InQuotes());
			string executable = EmscriptenPaths.Python.ToString();
			if (PlatformUtils.IsWindows())
			{
				executable = EmscriptenPaths.Python.InQuotes();
			}
			return new CppProgramBuilder.LinkerInvocation
			{
				ExecuteArgs = new Shell.ExecuteArgs
				{
					Executable = executable,
					Arguments = list2.Append("@" + shortPathName.InQuotes()).SeparateWithSpaces(),
					EnvVars = EnvVars(),
					WorkingDirectory = text
				},
				ArgumentsInfluencingOutcome = list2,
				FilesInfluencingOutcome = list
			};
		}

		public override IEnumerable<string> CompilerFlagsFor(CppCompilationInstruction cppCompilationInstruction)
		{
			yield return "-E";
			if (cppCompilationInstruction.SourceFile.HasExtension(".c"))
			{
				yield return EmscriptenPaths.Emcc.InQuotes();
				yield return "-x c";
			}
			else
			{
				yield return EmscriptenPaths.Emcpp.InQuotes();
			}
			foreach (string item in ChooseCompilerFlags(cppCompilationInstruction, DefaultCompilerFlags))
			{
				yield return item;
			}
			foreach (string define in cppCompilationInstruction.Defines)
			{
				yield return "-D" + define;
			}
			foreach (NPath includePath in cppCompilationInstruction.IncludePaths)
			{
				yield return "-I\"" + GetShortPathName(Path.GetFullPath(includePath.ToString())) + "\"";
			}
			yield return GetShortPathName(Path.GetFullPath(cppCompilationInstruction.SourceFile.ToString())).InQuotes();
		}

		public override NPath CompilerExecutableFor(NPath sourceFile)
		{
			return EmscriptenPaths.Python;
		}

		private IEnumerable<string> DefaultLinkerFlags(IEnumerable<NPath> staticLibraries, IEnumerable<NPath> dynamicLibraries, NPath outputFile)
		{
			if (base.BuildConfiguration == BuildConfiguration.Debug)
			{
				if (DeepDebugging)
				{
					yield return "-O0";
					yield return "-g";
					yield return "-s ALIASING_FUNCTION_POINTERS=0";
					yield return "-s SAFE_HEAP=3";
				}
				else
				{
					yield return "-O1";
					yield return "-g0";
				}
			}
			else if (base.BuildConfiguration == BuildConfiguration.Release)
			{
				yield return "-O3";
			}
			else if (base.BuildConfiguration == BuildConfiguration.ReleasePlus)
			{
				yield return "-O3";
				yield return "--llvm-lto 3";
			}
			else if (base.BuildConfiguration == BuildConfiguration.ReleaseSize)
			{
				yield return "-Oz";
				yield return "-g0";
				yield return "-s NO_FILESYSTEM=1";
				yield return "--llvm-lto 3";
				yield return "-s ELIMINATE_DUPLICATE_FUNCTIONS=1";
			}
		}

		private IEnumerable<string> DefaultCompilerFlags(CppCompilationInstruction cppCompilationInstruction)
		{
			yield return "-Wno-unused-value";
			yield return "-Wno-invalid-offsetof";
			yield return "-nostdinc";
			yield return "-fno-strict-overflow";
			yield return "-Wno-null-conversion";
			if (cppCompilationInstruction.SourceFile.HasExtension(".cpp"))
			{
				yield return "-std=c++11";
			}
			if (_targetingThreads)
			{
				yield return "-s";
				yield return "USE_PTHREADS=1";
			}
			if (base.BuildConfiguration == BuildConfiguration.Debug)
			{
				if (DeepDebugging)
				{
					yield return "-O0";
					yield return "-g";
					yield return "-s ALIASING_FUNCTION_POINTERS=0";
					yield return "-s SAFE_HEAP=3";
				}
				else
				{
					yield return "-O1";
					yield return "-g0";
				}
			}
			else if (base.BuildConfiguration == BuildConfiguration.Release || base.BuildConfiguration == BuildConfiguration.ReleasePlus)
			{
				yield return "-O3";
			}
			else if (base.BuildConfiguration == BuildConfiguration.ReleaseSize)
			{
				yield return "-Oz";
				yield return "-g0";
			}
			if (_disableExceptions)
			{
				yield return "-fno-rtti";
				yield return "-fno-exceptions";
			}
		}
	}
}
