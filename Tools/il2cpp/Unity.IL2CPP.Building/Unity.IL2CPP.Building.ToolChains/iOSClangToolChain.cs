using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NiceIO;
using Unity.IL2CPP.Common;

namespace Unity.IL2CPP.Building.ToolChains
{
	public class iOSClangToolChain : CppToolChain
	{
		private readonly bool _useDependenciesToolChain;

		private Func<string, IEnumerable<string>> AdditionalCompilerOptionsForSourceFile;

		public override bool SupportsMapFileParser => false;

		public override string MapFileParserFormat => "Clang";

		public override string DynamicLibraryExtension => ".dylib";

		public override string StaticLibraryExtension => ".a";

		public iOSClangToolChain(Architecture architecture, BuildConfiguration buildConfiguration, bool treatWarningsAsErrors, bool useDependenciesToolChain)
			: base(architecture, buildConfiguration)
		{
			if (treatWarningsAsErrors)
			{
				AdditionalCompilerOptionsForSourceFile = FlagsToMakeWarningsErrorsFor;
			}
			_useDependenciesToolChain = useDependenciesToolChain;
		}

		public override IEnumerable<string> ToolChainDefines()
		{
			yield break;
		}

		public override IEnumerable<NPath> ToolChainIncludePaths()
		{
			yield return iOSDevSDKPath().Combine("usr/include");
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
			return PlatformUtils.IsOSX();
		}

		public override string GetCannotBuildInCurrentEnvironmentErrorMessage()
		{
			return "C++ code builder is unable to build C++ code. In order to build C++ code for iOS, you must be running on MacOS.";
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
				list2.Add("-dynamiclib");
			}
			list2.AddRange(ChooseLinkerFlags(staticLibraries, dynamicLibraries, outputFile, specifiedLinkerFlags, DefaultLinkerFlags));
			list.AddRange(staticLibraries);
			list.AddRange(dynamicLibraries);
			list2.AddRange(staticLibraries.InQuotes());
			list2.AddRange(ToolChainStaticLibraries().InQuotes());
			list2.AddRange(dynamicLibraries.InQuotes());
			list2.AddRange(objectFiles.InQuotes());
			return new CppProgramBuilder.LinkerInvocation
			{
				ExecuteArgs = new Shell.ExecuteArgs
				{
					Executable = iOSBuildToolPath("usr/bin/clang++").ToString(),
					Arguments = list2.SeparateWithSpaces()
				},
				ArgumentsInfluencingOutcome = list2,
				FilesInfluencingOutcome = objectFiles.Concat(staticLibraries)
			};
		}

		public override NPath CompilerExecutableFor(NPath sourceFile)
		{
			return iOSBuildToolPath((sourceFile.HasExtension(".c") || sourceFile.HasExtension(".m")) ? "usr/bin/clang" : "usr/bin/clang++");
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
			else
			{
				yield return "-O3";
			}
			if (base.BuildConfiguration == BuildConfiguration.ReleasePlus)
			{
				yield return "-flto";
			}
			yield return "-miphoneos-version-min=8.0";
			yield return "-arch";
			yield return (base.Architecture.Bits == 32) ? "armv7" : "arm64";
			yield return "-isysroot";
			yield return iOSDevSDKPath().ToString();
			if (cppCompilationInstruction.SourceFile.HasExtension(".cpp"))
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
			}
			if (AdditionalCompilerOptionsForSourceFile == null)
			{
				yield break;
			}
			foreach (string item in AdditionalCompilerOptionsForSourceFile(cppCompilationInstruction.SourceFile.ToString()))
			{
				yield return item;
			}
		}

		private NPath iOSDevSDKPath()
		{
			if (_useDependenciesToolChain)
			{
				Unity.IL2CPP.Common.ToolChains.iOS.AssertReadyToUse();
				return Unity.IL2CPP.Common.ToolChains.iOS.iOSSDKDirectory;
			}
			NPath sdksParentFolder = new NPath("/Applications/Xcode.app/Contents/Developer/Platforms/iPhoneOS.platform/Developer/SDKs");
			return new string[2] { "iPhoneOS10.2.sdk", "iPhoneOS.sdk" }.Select((string sdk) => sdksParentFolder.Combine(sdk)).First((NPath sdk) => sdk.DirectoryExists());
		}

		private NPath iOSBuildToolPath(string path)
		{
			if (_useDependenciesToolChain)
			{
				Unity.IL2CPP.Common.ToolChains.iOS.AssertReadyToUse();
				return Unity.IL2CPP.Common.ToolChains.iOS.ToolPath(path);
			}
			return new NPath("/" + path);
		}

		private IEnumerable<string> DefaultLinkerFlags(IEnumerable<NPath> staticLibraries, IEnumerable<NPath> dynamicLibaries, NPath outputFile)
		{
			yield return "-arch";
			yield return (base.Architecture.Bits == 32) ? "armv7" : "arm64";
			yield return "-lSystem";
			yield return "-lc++";
			yield return "-lpthread";
			yield return "-miphoneos-version-min=8.0";
			yield return "-isysroot";
			yield return iOSDevSDKPath().ToString();
			yield return "-stdlib=libc++";
			if (base.BuildConfiguration == BuildConfiguration.ReleasePlus)
			{
				yield return "-flto";
			}
		}

		private static IEnumerable<string> FlagsToMakeWarningsErrorsFor(string sourceFile)
		{
			if (sourceFile.Contains("generatedcpp") && !sourceFile.Contains("pinvoke-targets.cpp"))
			{
				yield return "-Werror";
				yield return "-Wno-trigraphs";
				yield return "-Wno-tautological-compare";
				yield return "-Wswitch";
				yield return "-Wno-invalid-offsetof";
				if (sourceFile.Contains("myfile.cpp"))
				{
					yield return "-Wsign-compare";
				}
				yield return "-Wno-null-conversion";
				yield return "-Wno-unused-value";
				yield return "-Wno-missing-declarations";
			}
		}

		public override string GetToolchainInfoForOutput()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine(base.GetToolchainInfoForOutput());
			stringBuilder.AppendLine($"\tiOS Dev SDK: {iOSDevSDKPath()}");
			return stringBuilder.ToString();
		}
	}
}
