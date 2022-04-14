using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NiceIO;
using Unity.IL2CPP.Building.ToolChains.Linux;
using Unity.IL2CPP.Common;

namespace Unity.IL2CPP.Building.ToolChains
{
	public class LinuxClangToolChain : CppToolChain
	{
		private readonly LinuxClangSdk _sdk;

		private readonly bool _treatWarningsAsErrors;

		private BuildingOptions _buildingOptions;

		public override string DynamicLibraryExtension => ".so";

		public override string StaticLibraryExtension => ".a";

		public override bool SupportsMapFileParser => false;

		public LinuxClangToolChain(BuildingOptions buildingOptions)
			: base(buildingOptions.Architecture, buildingOptions.Configuration)
		{
			_treatWarningsAsErrors = buildingOptions.TreatWarningsAsErrors;
			_buildingOptions = buildingOptions;
			if (buildingOptions.SysrootPath != null || buildingOptions.ToolChainPath != null)
			{
				_sdk = new LinuxClangSdk(buildingOptions.SysrootPath, buildingOptions.ToolChainPath);
			}
			else if (CommonPaths.Il2CppDependenciesAvailable)
			{
				_sdk = LinuxClangSdk.GetDependenciesInstallation();
			}
			else
			{
				_sdk = LinuxClangSdk.GetSystemInstallation();
			}
		}

		public override IEnumerable<NPath> ToolChainIncludePaths()
		{
			foreach (NPath includePath in _sdk.IncludePaths)
			{
				yield return includePath;
			}
			foreach (NPath additionalIncludeDirectory in _buildingOptions.AdditionalIncludeDirectories)
			{
				yield return additionalIncludeDirectory;
			}
		}

		public override IEnumerable<NPath> ToolChainLibraryPaths()
		{
			foreach (NPath libraryPath in _sdk.LibraryPaths)
			{
				yield return libraryPath;
			}
			foreach (NPath additionalLinkDirectory in _buildingOptions.AdditionalLinkDirectories)
			{
				yield return additionalLinkDirectory;
			}
		}

		public override IEnumerable<string> ToolChainDefines()
		{
			yield return "__linux__";
			yield return "LINUX";
			yield return "_GNU_SOURCE";
		}

		public override string ExecutableExtension()
		{
			return "";
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

		public override bool CanBuildInCurrentEnvironment()
		{
			return _sdk.CanBuildCode();
		}

		public override string GetCannotBuildInCurrentEnvironmentErrorMessage()
		{
			return "C++ code builder is unable to build C++ code for Linux: " + _sdk.GetReasonCannotBuildCode();
		}

		protected override string GetInterestingOutputFromCompilationShellResult(Shell.ExecuteResult shellResult)
		{
			return shellResult.StdErr;
		}

		protected override string GetInterestingOutputFromLinkerShellResult(Shell.ExecuteResult shellResult)
		{
			return shellResult.StdErr;
		}

		public override bool DynamicLibrariesHaveToSitNextToExecutable()
		{
			return true;
		}

		protected virtual List<string> GetLinkerArgs(IEnumerable<NPath> objectFiles, NPath outputFile, IEnumerable<NPath> staticLibraries, IEnumerable<NPath> dynamicLibraries, IEnumerable<string> specifiedLinkerFlags)
		{
			List<string> list = new List<string>();
			list.AddRange(specifiedLinkerFlags);
			foreach (string item in _sdk.LinkerOptions())
			{
				list.Add(item);
			}
			list.Add("-o" + outputFile.InQuotes());
			list.Add("-fPIC");
			if (outputFile.HasExtension(DynamicLibraryExtension))
			{
				list.Add("-shared");
			}
			else
			{
				list.Add("-Xlinker --export-dynamic");
			}
			if (base.BuildConfiguration == BuildConfiguration.ReleasePlus)
			{
				list.Add("-flto");
			}
			if (base.BuildConfiguration != 0)
			{
				list.Add("-Xlinker -gc-sections");
			}
			list.AddRange(objectFiles.InQuotes());
			list.AddRange(staticLibraries.InQuotes());
			foreach (string item2 in ToolChainStaticLibraries())
			{
				list.Add("-l" + item2);
			}
			foreach (NPath staticLibrary in staticLibraries)
			{
				list.Add("-Xlinker -L" + staticLibrary.Parent.InQuotes());
				list.Add("-l:" + staticLibrary.FileName.InQuotes());
			}
			foreach (NPath dynamicLibrary in dynamicLibraries)
			{
				list.Add("-l" + dynamicLibrary);
			}
			foreach (NPath item3 in ToolChainLibraryPaths())
			{
				list.Add("-L" + item3);
			}
			foreach (string item4 in _sdk.LinkerOptions())
			{
				list.Add(item4);
			}
			list.Add("-Xlinker --no-undefined");
			if (dynamicLibraries.Count() > 1)
			{
				throw new ArgumentException("We've never tried to link to more than one shared library on linux. there be rpath dragons");
			}
			if (dynamicLibraries.Any())
			{
				list.Add("-Wl,-rpath,'$ORIGIN'");
			}
			list.AddRange(new string[2] { "-lpthread", "-ldl" });
			return list;
		}

		public override CppProgramBuilder.LinkerInvocation MakeLinkerInvocation(IEnumerable<NPath> objectFiles, NPath outputFile, IEnumerable<NPath> staticLibraries, IEnumerable<NPath> dynamicLibraries, IEnumerable<string> specifiedLinkerFlags, CppToolChainContext toolChainContext)
		{
			List<string> linkerArgs = GetLinkerArgs(objectFiles, outputFile, staticLibraries, dynamicLibraries, specifiedLinkerFlags);
			string text = linkerArgs.SeparateWithSpaces();
			text = text.Replace("\\", "\\\\");
			string text2 = TempDir.Il2CppTemporaryDirectoryRoot.Combine("clangargs_" + HashTools.HashOf(text) + ".txt");
			TempDir.Il2CppTemporaryDirectoryRoot.EnsureDirectoryExists();
			if (!File.Exists(text2))
			{
				File.WriteAllText(text2, text);
			}
			return new CppProgramBuilder.LinkerInvocation
			{
				ExecuteArgs = new Shell.ExecuteArgs
				{
					Executable = LinkerExecutableFor().ToString(),
					Arguments = "@" + text2
				},
				ArgumentsInfluencingOutcome = linkerArgs,
				FilesInfluencingOutcome = objectFiles.Concat(staticLibraries)
			};
		}

		public virtual NPath LinkerExecutableFor()
		{
			return _sdk.GetCppCompilerPath();
		}

		public override NPath CompilerExecutableFor(NPath sourceFile)
		{
			if (!sourceFile.HasExtension(".cpp"))
			{
				return _sdk.GetCCompilerPath();
			}
			return _sdk.GetCppCompilerPath();
		}

		public override IEnumerable<string> CompilerFlagsFor(CppCompilationInstruction cppCompilationInstruction)
		{
			foreach (string item in _sdk.CompilerOptions())
			{
				yield return item;
			}
			foreach (string define in cppCompilationInstruction.Defines)
			{
				yield return "-D" + define;
			}
			foreach (NPath item2 in ToolChainIncludePaths())
			{
				yield return "-I" + item2.InQuotes();
			}
			foreach (NPath includePath in cppCompilationInstruction.IncludePaths)
			{
				yield return "-I" + includePath.InQuotes();
			}
			foreach (string item3 in ChooseCompilerFlags(cppCompilationInstruction, DefaultCompilerFlagsFor))
			{
				yield return item3;
			}
			yield return cppCompilationInstruction.SourceFile.InQuotes();
		}

		private IEnumerable<string> DefaultCompilerFlagsFor(CppCompilationInstruction cppCompilationInstruction)
		{
			foreach (string item in _sdk.CompilerOptions())
			{
				yield return item;
			}
			yield return "-g";
			yield return "-c";
			yield return "-fvisibility=hidden";
			yield return "-fvisibility-inlines-hidden";
			yield return "-fno-strict-overflow";
			yield return "-fexceptions";
			yield return "-fno-rtti";
			yield return "-ffunction-sections";
			yield return "-fdata-sections";
			yield return "-fPIC";
			yield return "-pthread";
			if (cppCompilationInstruction.SourceFile.ExtensionWithDot.Equals(".c", StringComparison.OrdinalIgnoreCase))
			{
				yield return "-std=c11";
			}
			else
			{
				yield return "-std=c++11";
			}
			foreach (string item2 in _sdk.CompilerOptions())
			{
				yield return item2;
			}
			if (base.Architecture is x64Architecture || base.Architecture is x86Architecture)
			{
				yield return "-m" + base.Architecture.Bits;
			}
			yield return "-Wno-null-conversion";
			if (base.BuildConfiguration == BuildConfiguration.Debug)
			{
				yield return "-O0";
			}
			else if (base.BuildConfiguration == BuildConfiguration.ReleaseSize)
			{
				yield return "-Os";
			}
			else
			{
				yield return "-O3";
			}
			if (base.Architecture.Bits == 64)
			{
				yield return "-mcx16";
			}
			if (base.BuildConfiguration == BuildConfiguration.ReleasePlus)
			{
				yield return "-flto";
			}
			if (cppCompilationInstruction.TreatWarningsAsErrors && _treatWarningsAsErrors && !cppCompilationInstruction.SourceFile.FileName.Contains("pinvoke-targets.cpp"))
			{
				yield return "-Werror";
			}
			foreach (string item3 in GetFlagsToDisableWarningsFor(cppCompilationInstruction.SourceFile))
			{
				yield return item3;
			}
		}

		private IEnumerable<string> GetFlagsToDisableWarningsFor(NPath sourceFile)
		{
			if (!sourceFile.ToString().Contains("pinvoke-targets.cpp") && !sourceFile.ToString().Contains("MapFileParser") && !sourceFile.ToString().Replace('\\', '/').Contains("external/"))
			{
				yield return "-Wno-extern-initializer";
				yield return "-Wno-trigraphs";
				yield return "-Wno-tautological-compare";
				yield return "-Wswitch";
				yield return "-Wno-invalid-offsetof";
				yield return "-Wno-unused-value";
				yield return "-Wno-null-conversion";
				if (sourceFile.FileName.Contains("myfile.cpp"))
				{
					yield return "-Wsign-compare";
				}
			}
		}
	}
}
