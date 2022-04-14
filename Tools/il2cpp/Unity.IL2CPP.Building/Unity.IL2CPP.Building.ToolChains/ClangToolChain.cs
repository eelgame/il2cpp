using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NiceIO;
using Unity.IL2CPP.Building.BuildDescriptions;
using Unity.IL2CPP.Building.Hashing;
using Unity.IL2CPP.Common;

namespace Unity.IL2CPP.Building.ToolChains
{
	public class ClangToolChain : CppToolChain
	{
		private readonly bool _treatWarningsAsErrors;

		private HeaderFileHashProvider _pchHashProvider;

		private readonly bool _useDependenciesToolChain;

		private readonly bool _disableExceptions;

		private readonly string _showIncludesForFile;

		private NPath _pchCHeaderFile;

		private NPath _pchCppHeaderFile;

		public override string DynamicLibraryExtension => ".dylib";

		public override string StaticLibraryExtension => ".a";

		public ClangToolChain(Architecture architecture, BuildConfiguration buildConfiguration, bool treatWarningsAsErrors, bool useDependenciesToolChain, bool disableExceptions = false, string showIncludesForFile = "")
			: base(architecture, buildConfiguration)
		{
			_treatWarningsAsErrors = treatWarningsAsErrors;
			_pchHashProvider = new HeaderFileHashProvider();
			_useDependenciesToolChain = useDependenciesToolChain;
			_disableExceptions = disableExceptions;
			_showIncludesForFile = showIncludesForFile;
		}

		public override IEnumerable<string> ToolChainDefines()
		{
			yield return "TARGET_MACH";
			yield return "PLATFORM_MACOSX";
		}

		public override IEnumerable<NPath> ToolChainIncludePaths()
		{
			yield return MacDevSDKPath().Combine("usr/include");
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
			return "";
		}

		public override bool CanBuildInCurrentEnvironment()
		{
			if (PlatformUtils.IsOSX() && XcodeInstallation.Exists)
			{
				return XcodeInstallation.SDKPath != null;
			}
			return false;
		}

		public override string GetCannotBuildInCurrentEnvironmentErrorMessage()
		{
			if (!PlatformUtils.IsOSX())
			{
				return "C++ code builder is unable to build C++ code. In order to build C++ code for MacOS, you must be running on MacOS.";
			}
			if (!XcodeInstallation.Exists)
			{
				return string.Format("{0} Please install Xcode at {1}", "C++ code builder is unable to build C++ code.", XcodeInstallation.SupportedPath);
			}
			if (XcodeInstallation.SDKPath == null)
			{
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.AppendLine("C++ code builder is unable to build C++ code. Please ensure the macOS SDK is installed in one of the following directories:");
				stringBuilder.AppendLine(XcodeInstallation.PlatformSupportedPath);
				return stringBuilder.ToString();
			}
			return "C++ code builder is unable to build C++ code.";
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
			List<string> list2 = new List<string>
			{
				"-o",
				outputFile.InQuotes()
			};
			if (outputFile.HasExtension(DynamicLibraryExtension))
			{
				list2.Add("-shared");
			}
			if (base.BuildConfiguration == BuildConfiguration.AddressSanitizer)
			{
				list2.AddRange(ChooseLinkerFlags(staticLibraries, dynamicLibraries, outputFile, Enumerable.Empty<string>(), DefaultLinkerFlagsForAddressSanitizer));
			}
			else if (base.BuildConfiguration == BuildConfiguration.UndefinedBehaviorSanitizer)
			{
				list2.AddRange(ChooseLinkerFlags(staticLibraries, dynamicLibraries, outputFile, Enumerable.Empty<string>(), DefaultLinkerFlagsForUndefinedBehaviorSanitizer));
			}
			else if (base.BuildConfiguration == BuildConfiguration.ThreadSanitizer)
			{
				list2.AddRange(ChooseLinkerFlags(staticLibraries, dynamicLibraries, outputFile, Enumerable.Empty<string>(), DefaultLinkerFlagsForThreadSanitizer));
			}
			else
			{
				list2.AddRange(ChooseLinkerFlags(staticLibraries, dynamicLibraries, outputFile, Enumerable.Empty<string>(), DefaultLinkerFlags));
			}
			foreach (string specifiedLinkerFlag in specifiedLinkerFlags)
			{
				list2.Add("-Xlinker");
				list2.Add(specifiedLinkerFlag);
			}
			list.AddRange(staticLibraries);
			list.AddRange(dynamicLibraries);
			list2.AddRange(staticLibraries.InQuotes());
			list2.AddRange(ToolChainStaticLibraries().InQuotes());
			list2.AddRange(dynamicLibraries.InQuotes());
			list2.AddRange(objectFiles.InQuotes());
			string path = "usr/bin/clang";
			return new CppProgramBuilder.LinkerInvocation
			{
				ExecuteArgs = new Shell.ExecuteArgs
				{
					Executable = MacBuildToolPath(path).ToString(),
					Arguments = list2.SeparateWithSpaces()
				},
				ArgumentsInfluencingOutcome = list2,
				FilesInfluencingOutcome = objectFiles.Concat(staticLibraries)
			};
		}

		public override NPath CompilerExecutableFor(NPath sourceFile)
		{
			return MacBuildToolPath((sourceFile.HasExtension(".c") || sourceFile.HasExtension(".m")) ? "usr/bin/clang" : "usr/bin/clang++");
		}

		public override IEnumerable<string> CompilerFlagsFor(CppCompilationInstruction cppCompilationInstruction)
		{
			foreach (string define in cppCompilationInstruction.Defines)
			{
				yield return "-D" + define;
			}
			foreach (NPath includePath in cppCompilationInstruction.IncludePaths)
			{
				yield return string.Concat("-I\"", includePath, "\"");
			}
			foreach (string item in ChooseCompilerFlags(cppCompilationInstruction, DefaultCompilerFlags))
			{
				yield return item;
			}
			yield return cppCompilationInstruction.SourceFile.InQuotes();
		}

		private IEnumerable<string> DefaultCompilerFlags(CppCompilationInstruction cppCompilationInstruction)
		{
			yield return "-g";
			yield return "-c";
			yield return "-fvisibility=hidden";
			yield return "-fno-strict-overflow";
			if (base.BuildConfiguration == BuildConfiguration.Debug)
			{
				yield return "-O0";
			}
			else if (base.BuildConfiguration == BuildConfiguration.AddressSanitizer)
			{
				foreach (string item in DefaultCompilerFlagsForAddressSanitizer())
				{
					yield return item;
				}
			}
			else if (base.BuildConfiguration == BuildConfiguration.UndefinedBehaviorSanitizer)
			{
				foreach (string item2 in DefaultCompilerFlagsForUndefinedBehaviorSanitizer())
				{
					yield return item2;
				}
			}
			else if (base.BuildConfiguration == BuildConfiguration.ThreadSanitizer)
			{
				foreach (string item3 in DefaultCompilerFlagsForThreadSanitizer())
				{
					yield return item3;
				}
			}
			else if (base.BuildConfiguration == BuildConfiguration.ReleaseSize)
			{
				yield return "-Oz";
			}
			else
			{
				yield return "-O3";
			}
			if (base.BuildConfiguration == BuildConfiguration.ReleasePlus)
			{
				yield return "-flto";
			}
			yield return "-mmacosx-version-min=" + GetMinMacOSVersion();
			yield return "-arch";
			yield return GetClangArchitectureName();
			yield return "-isysroot";
			yield return MacDevSDKPath().ToString();
			yield return string.Format("-I{0}", MacDevSDKPath().Combine("malloc"));
			if (base.Architecture is x64Architecture)
			{
				yield return "-mcx16";
			}
			if (_disableExceptions)
			{
				yield return "-fno-exceptions";
				yield return "-fno-rtti";
			}
			else
			{
				yield return "-fexceptions";
			}
			if (cppCompilationInstruction.SourceFile.HasExtension(".cpp") || cppCompilationInstruction.SourceFile.HasExtension(".hpp"))
			{
				yield return "-std=c++11";
				yield return "-stdlib=libc++";
			}
			if (cppCompilationInstruction.SourceFile.HasExtension(".m"))
			{
				yield return "-fobjc-arc";
				yield return "-ObjC";
			}
			if (cppCompilationInstruction.SourceFile.HasExtension(".mm"))
			{
				yield return "-fobjc-arc";
				yield return "-ObjC++";
				yield return "-stdlib=libc++";
				yield return "-std=c++11";
			}
			if (cppCompilationInstruction.TreatWarningsAsErrors && _treatWarningsAsErrors)
			{
				foreach (string item4 in FlagsToMakeWarningsErrorsFor(cppCompilationInstruction.SourceFile.ToString()))
				{
					yield return item4;
				}
			}
			if (string.Equals(cppCompilationInstruction.SourceFile.FileName, _showIncludesForFile, StringComparison.OrdinalIgnoreCase))
			{
				yield return "-H";
			}
			if (HasPrecompiledHeader(cppCompilationInstruction.SourceFile))
			{
				if (_pchCHeaderFile != null && cppCompilationInstruction.SourceFile.ExtensionWithDot.Equals(".c"))
				{
					yield return "-include";
					yield return _pchCHeaderFile.ChangeExtension("").InQuotes();
				}
				if (_pchCppHeaderFile != null && cppCompilationInstruction.SourceFile.ExtensionWithDot.Equals(".cpp"))
				{
					yield return "-include";
					yield return _pchCppHeaderFile.ChangeExtension("").InQuotes();
				}
			}
		}

		private string GetClangArchitectureName()
		{
			if (base.Architecture is x64Architecture)
			{
				return "x86_64";
			}
			if (base.Architecture is x86Architecture)
			{
				return "i386";
			}
			if (base.Architecture is ARM64Architecture)
			{
				return "arm64";
			}
			throw new NotSupportedException("Unknown architecture: " + base.Architecture);
		}

		private bool HasPrecompiledHeader(NPath sourceFile)
		{
			using (StreamReader streamReader = new StreamReader(sourceFile))
			{
				string text = streamReader.ReadLine();
				while (text != null && string.IsNullOrWhiteSpace(text))
				{
					text = streamReader.ReadLine();
				}
				if (text != null)
				{
					return text.Equals("#include \"pch-c.h\"") || text.Equals("#include \"pch-cpp.hpp\"");
				}
			}
			return false;
		}

		private static IEnumerable<string> DefaultCompilerFlagsForAddressSanitizer()
		{
			yield return "-fsanitize=address";
			foreach (string item in DefaultCompilerFlagsForAnySanitizer())
			{
				yield return item;
			}
		}

		private static string FormatUndefinedBehaviorSanitizerChecksArgument()
		{
			string[] obj = new string[16]
			{
				"bool", "bounds", "enum", "float-divide-by-zero", "integer-divide-by-zero", "nonnull-attribute", "nullability-arg", "nullability-assign", "nullability-return", "pointer-overflow",
				"return", "returns-nonnull-attribute", "shift", "signed-integer-overflow", "unreachable", "vla-bound"
			};
			StringBuilder stringBuilder = new StringBuilder();
			string[] array = obj;
			foreach (string text in array)
			{
				stringBuilder.Append("-fsanitize=" + text + " ");
			}
			return stringBuilder.ToString();
		}

		private static IEnumerable<string> DefaultCompilerFlagsForUndefinedBehaviorSanitizer()
		{
			yield return FormatUndefinedBehaviorSanitizerChecksArgument();
			foreach (string item in DefaultCompilerFlagsForAnySanitizer())
			{
				yield return item;
			}
		}

		private static IEnumerable<string> DefaultCompilerFlagsForThreadSanitizer()
		{
			yield return "-fsanitize=thread";
			yield return $"-fsanitize-blacklist={CommonPaths.Il2CppRoot}/thread-sanitizer-special-cases.txt";
			foreach (string item in DefaultCompilerFlagsForAnySanitizer())
			{
				yield return item;
			}
		}

		private static IEnumerable<string> DefaultCompilerFlagsForAnySanitizer()
		{
			yield return "-fno-sanitize-recover=all";
			yield return "-fno-omit-frame-pointer";
			yield return "-fno-optimize-sibling-calls";
			yield return "-O0";
		}

		private NPath MacDevSDKPath()
		{
			if (_useDependenciesToolChain)
			{
				Unity.IL2CPP.Common.ToolChains.OSX.AssertReadyToUse();
				return Unity.IL2CPP.Common.ToolChains.OSX.MacSDKDirectory;
			}
			return XcodeInstallation.SDKPath;
		}

		private NPath MacBuildToolPath(string path)
		{
			if (_useDependenciesToolChain)
			{
				Unity.IL2CPP.Common.ToolChains.OSX.AssertReadyToUse();
				return Unity.IL2CPP.Common.ToolChains.OSX.ToolPath(path);
			}
			return new NPath("/" + path);
		}

		private string GetMinMacOSVersion()
		{
			if (base.Architecture is x64Architecture)
			{
				return "10.12";
			}
			if (base.Architecture is ARM64Architecture)
			{
				return "11.0";
			}
			throw new NotSupportedException("Unknown architecture: " + base.Architecture);
		}

		private IEnumerable<string> GetRawLinkerFlags(IEnumerable<NPath> staticLibraries, IEnumerable<NPath> dynamicLibaries, NPath outputFile)
		{
			yield return "-arch";
			yield return GetClangArchitectureName();
			yield return "-macosx_version_min";
			yield return GetMinMacOSVersion();
			yield return "-lSystem";
			yield return "-lc++";
			yield return "-lpthread";
			if (base.BuildConfiguration == BuildConfiguration.ReleasePlus)
			{
				yield return "-object_path_lto";
			}
			if (base.BuildConfiguration != 0)
			{
				yield return "-dead_strip";
			}
		}

		private IEnumerable<string> DefaultLinkerFlagsForAddressSanitizer(IEnumerable<NPath> staticLibraries, IEnumerable<NPath> dynamicLibaries, NPath outputFile)
		{
			yield return "-fsanitize=address";
			foreach (string item in DefaultLinkerFlagsForAnySanitizer(staticLibraries, dynamicLibaries, outputFile))
			{
				yield return item;
			}
		}

		private IEnumerable<string> DefaultLinkerFlagsForUndefinedBehaviorSanitizer(IEnumerable<NPath> staticLibraries, IEnumerable<NPath> dynamicLibaries, NPath outputFile)
		{
			yield return FormatUndefinedBehaviorSanitizerChecksArgument();
			foreach (string item in DefaultLinkerFlagsForAnySanitizer(staticLibraries, dynamicLibaries, outputFile))
			{
				yield return item;
			}
		}

		private IEnumerable<string> DefaultLinkerFlagsForThreadSanitizer(IEnumerable<NPath> staticLibraries, IEnumerable<NPath> dynamicLibaries, NPath outputFile)
		{
			yield return "-fsanitize=thread";
			foreach (string item in DefaultLinkerFlagsForAnySanitizer(staticLibraries, dynamicLibaries, outputFile))
			{
				yield return item;
			}
		}

		private IEnumerable<string> DefaultLinkerFlagsForAnySanitizer(IEnumerable<NPath> staticLibraries, IEnumerable<NPath> dynamicLibaries, NPath outputFile)
		{
			yield return "-fno-sanitize-recover=all";
			foreach (string item in DefaultLinkerFlags(staticLibraries, dynamicLibaries, outputFile))
			{
				yield return item;
			}
		}

		private IEnumerable<string> DefaultLinkerFlags(IEnumerable<NPath> staticLibraries, IEnumerable<NPath> dynamicLibaries, NPath outputFile)
		{
			yield return "-isysroot";
			yield return MacDevSDKPath().ToString();
			foreach (string linkerFlag in GetRawLinkerFlags(staticLibraries, dynamicLibaries, outputFile))
			{
				yield return "-Xlinker";
				yield return linkerFlag;
			}
		}

		private static IEnumerable<string> FlagsToMakeWarningsErrorsFor(string sourceFile)
		{
			if (!sourceFile.Contains("pinvoke-targets.cpp"))
			{
				yield return "-Werror";
				yield return "-Wno-trigraphs";
				yield return "-Wno-tautological-compare";
				yield return "-Wswitch";
				yield return "-Wno-invalid-offsetof";
				yield return "-Wno-unused-value";
				yield return "-Wno-null-conversion";
				if (sourceFile.Contains("myfile.cpp"))
				{
					yield return "-Wsign-compare";
				}
				yield return "-Wno-missing-declarations";
			}
		}

		public override string GetToolchainInfoForOutput()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine(base.GetToolchainInfoForOutput());
			stringBuilder.AppendLine($"\tMac Dev SDK: {MacDevSDKPath()}");
			return stringBuilder.ToString();
		}

		public override void OnBeforeCompile(ProgramBuildDescription programBuildDescription, CppToolChainContext toolChainContext, HeaderFileHashProvider headerHashProvider, NPath workingDirectory, bool forceRebuild, bool verbose, bool includeFileNamesInHashes)
		{
			base.OnBeforeCompile(programBuildDescription, toolChainContext, headerHashProvider, workingDirectory, forceRebuild, verbose, includeFileNamesInHashes);
			CompilePch(programBuildDescription, workingDirectory, forceRebuild, verbose, includeFileNamesInHashes);
		}

		private void CompilePch(ProgramBuildDescription programBuildDescription, NPath workingDirectory, bool forceRebuild, bool verbose, bool includeFileNamesInHashes)
		{
			IL2CPPOutputBuildDescription il2CppOutputBuildDescription = programBuildDescription as IL2CPPOutputBuildDescription;
			if (il2CppOutputBuildDescription != null)
			{
				ParallelFor.Run(new Action[2]
				{
					delegate
					{
						InvokePchCompilation(programBuildDescription, workingDirectory, forceRebuild, verbose, includeFileNamesInHashes, il2CppOutputBuildDescription.PchCSourceFile);
					},
					delegate
					{
						InvokePchCompilation(programBuildDescription, workingDirectory, forceRebuild, verbose, includeFileNamesInHashes, il2CppOutputBuildDescription.PchCppSourceFile);
					}
				}, delegate(Action f)
				{
					f();
				});
			}
		}

		private void InvokePchCompilation(ProgramBuildDescription programBuildDescription, NPath workingDirectory, bool forceRebuild, bool verbose, bool includeFileNamesInHashes, NPath sourceFile)
		{
			if (sourceFile == null || !sourceFile.Exists())
			{
				return;
			}
			NPath sourceFile2 = (sourceFile.ExtensionWithDot.Equals(".c") ? sourceFile.ChangeExtension(".h") : sourceFile.ChangeExtension(".hpp"));
			IEnumerable<NPath> enumerable = ToolChainIncludePaths().Concat(programBuildDescription.AdditionalIncludePathsFor(sourceFile));
			NPath[] array = (enumerable as NPath[]) ?? enumerable.ToArray();
			CppCompilationInstruction cppCompilationInstruction = new CppCompilationInstruction
			{
				SourceFile = sourceFile2,
				Defines = ToolChainDefines().Concat(programBuildDescription.AdditionalDefinesFor(sourceFile)),
				IncludePaths = array,
				LumpPaths = Enumerable.Empty<NPath>(),
				CompilerFlags = programBuildDescription.AdditionalCompilerFlags,
				CacheDirectory = workingDirectory,
				TreatWarningsAsErrors = _treatWarningsAsErrors
			};
			CompilationInvocation compilationInvocation = new CompilationInvocation
			{
				CompilerExecutable = CompilerExecutableFor(sourceFile),
				SourceFile = sourceFile2,
				EnvVars = EnvVars(),
				Arguments = CompilerFlagsFor(cppCompilationInstruction).Concat(programBuildDescription.CompilerFlagsFor(cppCompilationInstruction)).Append("-Wno-pragma-once-outside-header")
			};
			string hashForAllHeaderFilesPossiblyInfluencingCompilation = _pchHashProvider.HashForAllIncludableFilesInDirectories(array, new string[2] { ".h", ".hpp" });
			string text = compilationInvocation.Hash(hashForAllHeaderFilesPossiblyInfluencingCompilation);
			NPath nPath = workingDirectory.Combine(text).ChangeExtension(sourceFile.ExtensionWithDot.Equals(".c") ? ".h.gch" : ".hpp.gch");
			if (includeFileNamesInHashes)
			{
				string text2 = cppCompilationInstruction.SourceFile.FileName.Replace('.', '_');
				nPath = workingDirectory.Combine(text2 + text).ChangeExtension(sourceFile.ExtensionWithDot.Equals(".c") ? ".h.gch" : ".hpp.gch");
			}
			compilationInvocation.Arguments = compilationInvocation.Arguments.Concat(OutputArgumentFor(nPath, sourceFile2));
			if (sourceFile.ExtensionWithDot.Equals(".c"))
			{
				_pchCHeaderFile = nPath;
				if (!forceRebuild && _pchCHeaderFile.Exists())
				{
					return;
				}
			}
			else if (sourceFile.ExtensionWithDot.Equals(".cpp"))
			{
				_pchCppHeaderFile = nPath;
				if (!forceRebuild && _pchCppHeaderFile.Exists())
				{
					return;
				}
			}
			Shell.ExecuteResult executeResult = compilationInvocation.Execute();
			if (executeResult.ExitCode != 0)
			{
				throw new BuilderFailedException(executeResult.StdOut + Environment.NewLine + executeResult.StdErr + Environment.NewLine + "Invocation was: " + compilationInvocation.Summary());
			}
			if (verbose)
			{
				ConsoleOutput.Info.WriteLine(executeResult.StdOut.Trim());
			}
		}
	}
}
