using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NiceIO;
using Unity.IL2CPP.Building.BuildDescriptions;
using Unity.IL2CPP.Building.Hashing;
using Unity.IL2CPP.Building.ToolChains.MsvcVersions;
using Unity.IL2CPP.Common;
using Unity.MiniProfiling;

namespace Unity.IL2CPP.Building.ToolChains
{
	public abstract class MsvcToolChain : CppToolChain
	{
		private class IdlCompilationResult
		{
			public bool Success { get; private set; }

			public string StdOut { get; private set; }

			public NPath OutputDirectory { get; private set; }

			public CompilationInvocation Invocation { get; private set; }

			public static IdlCompilationResult SuccessfulResult(NPath outputDirectory)
			{
				return new IdlCompilationResult
				{
					Success = true,
					OutputDirectory = outputDirectory
				};
			}

			public static IdlCompilationResult FromShellExecuteResult(Shell.ExecuteResult shellExecuteResult, CompilationInvocation invocation, NPath outputDirectory)
			{
				return new IdlCompilationResult
				{
					Success = (shellExecuteResult.ExitCode == 0),
					StdOut = (shellExecuteResult.StdOut + Environment.NewLine + shellExecuteResult.StdErr).Trim(),
					OutputDirectory = outputDirectory,
					Invocation = invocation
				};
			}
		}

		private readonly bool _treatWarningsAsErrors;

		private readonly bool _assemblyOutput;

		private readonly bool _disableExceptions;

		private readonly string _showIncludesForFile;

		private NPath _pchCHeaderFile;

		private NPath _pchCObjectFile;

		private NPath _pchCppHeaderFile;

		private NPath _pchCppObjectFile;

		public abstract MsvcInstallation MsvcInstallation { get; }

		public bool DontLinkCrt { get; set; }

		public override string DynamicLibraryExtension => ".dll";

		public override string StaticLibraryExtension => ".lib";

		public NPath LinkerPath => MsvcInstallation.GetVSToolPath(base.Architecture, "link.exe");

		protected NPath CompilerPath => MsvcInstallation.GetVSToolPath(base.Architecture, "cl.exe");

		public override IEnumerable<NPath> PrecompiledHeaderObjectFiles()
		{
			if (_pchCObjectFile != null)
			{
				yield return _pchCObjectFile;
			}
			if (_pchCppObjectFile != null)
			{
				yield return _pchCppObjectFile;
			}
		}

		protected MsvcToolChain(Architecture architecture, BuildConfiguration buildConfiguration, bool treatWarningsAsErrors, bool assemblyOutput, bool disableExceptions = false, string showIncludesForFile = "")
			: base(architecture, buildConfiguration)
		{
			_treatWarningsAsErrors = treatWarningsAsErrors;
			_assemblyOutput = assemblyOutput;
			_disableExceptions = disableExceptions;
			_showIncludesForFile = showIncludesForFile;
		}

		public override CppToolChainContext CreateToolChainContext()
		{
			MsvcToolChainContext msvcToolChainContext = new MsvcToolChainContext();
			msvcToolChainContext.TreatWarningsAsErrors = _treatWarningsAsErrors;
			PostProcessToolChainContext(msvcToolChainContext);
			return msvcToolChainContext;
		}

		protected virtual void PostProcessToolChainContext(MsvcToolChainContext context)
		{
		}

		public override IEnumerable<string> ToolChainDefines()
		{
			foreach (string item in ToolChainVcDefines())
			{
				yield return item;
			}
			foreach (string item2 in ToolChainSdkDefines())
			{
				yield return item2;
			}
		}

		protected virtual IEnumerable<string> ToolChainVcDefines()
		{
			yield return "_WIN32";
			yield return "WIN32";
			yield return "WIN32_THREADS";
			yield return "_WINDOWS";
			yield return "WINDOWS";
			yield return "_UNICODE";
			yield return "UNICODE";
			yield return "_CRT_SECURE_NO_WARNINGS";
			yield return "_SCL_SECURE_NO_WARNINGS";
			yield return "_WINSOCK_DEPRECATED_NO_WARNINGS";
			yield return "NOMINMAX";
			if (base.BuildConfiguration == BuildConfiguration.Debug)
			{
				yield return "_DEBUG";
				yield return "DEBUG";
				yield return "IL2CPP_DEBUG=1";
			}
			else
			{
				yield return "_NDEBUG";
				yield return "NDEBUG";
			}
			if (_disableExceptions || DontLinkCrt)
			{
				yield return "_HAS_EXCEPTIONS=0";
			}
			if (DontLinkCrt)
			{
				yield return "_ITERATOR_DEBUG_LEVEL=0";
			}
			if (base.Architecture is ARMv7Architecture)
			{
				yield return "__arm__";
			}
		}

		public virtual IEnumerable<string> ToolChainSdkDefines()
		{
			yield return "WINDOWS_SDK_BUILD_VERSION=" + MsvcInstallation.WindowsSDKBuildVersion;
		}

		public override IEnumerable<string> OutputArgumentFor(NPath objectFile, NPath sourceFile)
		{
			yield return "/Fo" + objectFile.InQuotes();
			if (IsAsmFile(sourceFile))
			{
				yield return sourceFile.InQuotes();
				yield break;
			}
			yield return "/Fd" + objectFile.ChangeExtension(".pdb").InQuotes();
			if (_assemblyOutput)
			{
				yield return "/Fa" + sourceFile.ChangeExtension("s").InQuotes();
			}
		}

		public override bool CanBuildInCurrentEnvironment()
		{
			try
			{
				return MsvcInstallation != null && MsvcInstallation.CanBuildCode(base.Architecture);
			}
			catch
			{
				return false;
			}
		}

		public override string ObjectExtension()
		{
			return ".obj";
		}

		public override string ExecutableExtension()
		{
			return ".exe";
		}

		private bool IsAsmFile(NPath file)
		{
			return file.HasExtension(".asm");
		}

		public override Dictionary<string, string> EnvVars()
		{
			return new Dictionary<string, string> { 
			{
				"PATH",
				MsvcInstallation.GetPathEnvVariable(base.Architecture)
			} };
		}

		public override CppProgramBuilder.LinkerInvocation MakeLinkerInvocation(IEnumerable<NPath> objectFiles, NPath outputFile, IEnumerable<NPath> staticLibraries, IEnumerable<NPath> dynamicLibraries, IEnumerable<string> specifiedLinkerFlags, CppToolChainContext toolChainContext)
		{
			List<NPath> list = new List<NPath>(objectFiles);
			list.AddRange(staticLibraries);
			MsvcToolChainContext msvcToolChainContext = (MsvcToolChainContext)toolChainContext;
			string text = outputFile.ExtensionWithDot.ToLower();
			if (text != ".dll" && text != ".exe")
			{
				outputFile = outputFile.Parent.Combine(outputFile.FileName + ".exe");
			}
			List<string> list2 = new List<string> { "/out:" + outputFile.InQuotes() };
			if (msvcToolChainContext.ManifestPath != null)
			{
				list2.Add("/MANIFESTUAC:NO");
				list2.Add("/MANIFEST:EMBED");
				list2.Add($"/MANIFESTINPUT:{msvcToolChainContext.ManifestPath.InQuotes()}");
				list.Add(msvcToolChainContext.ManifestPath);
			}
			if (msvcToolChainContext.ModuleDefinitionPath != null)
			{
				list2.Add($"/DEF:{msvcToolChainContext.ModuleDefinitionPath.InQuotes()}");
				list.Add(msvcToolChainContext.ModuleDefinitionPath);
			}
			NPath bestWorkingDirectory = PickBestDirectoryToUseAsWorkingDirectoryFromObjectFilePaths(objectFiles);
			IEnumerable<string> enumerable = objectFiles.Select((NPath p) => p.IsChildOf(bestWorkingDirectory) ? p.RelativeTo(bestWorkingDirectory).ToString() : p.ToString());
			list2.AddRange(ChooseLinkerFlags(staticLibraries, dynamicLibraries, outputFile, specifiedLinkerFlags, GetDefaultLinkerArgs));
			string tempFileName = Path.GetTempFileName();
			File.WriteAllText(tempFileName, enumerable.InQuotes().AggregateWithNewLine(), Encoding.UTF8);
			if (BuildShell.LogEnabled)
			{
				foreach (string item in enumerable)
				{
					BuildShell.AppendToCommandLog("ResponseFile: {0}: {1}", tempFileName, item);
				}
			}
			return new CppProgramBuilder.LinkerInvocation
			{
				ExecuteArgs = new Shell.ExecuteArgs
				{
					Arguments = list2.Append("@" + tempFileName.InQuotes()).SeparateWithSpaces(),
					Executable = LinkerPath.ToString(),
					EnvVars = EnvVars(),
					WorkingDirectory = bestWorkingDirectory.ToString()
				},
				ArgumentsInfluencingOutcome = list2,
				FilesInfluencingOutcome = list
			};
		}

		protected virtual IEnumerable<string> GetDefaultLinkerArgs(IEnumerable<NPath> staticLibraries, IEnumerable<NPath> dynamicLibraries, NPath outputFile)
		{
			yield return "/ignore:4206";
			if (MsvcInstallation.Version.Major >= 14 && base.BuildConfiguration == BuildConfiguration.Debug && Environment.GetEnvironmentVariable("UNITY_THISISABUILDMACHINE") != "1")
			{
				yield return "/DEBUG:FASTLINK";
			}
			else
			{
				yield return "/DEBUG";
			}
			yield return "/INCREMENTAL:NO";
			yield return "/LARGEADDRESSAWARE";
			yield return "/NXCOMPAT";
			yield return "/DYNAMICBASE";
			yield return "/NOLOGO";
			yield return "/TLBID:1";
			if (base.BuildConfiguration != 0)
			{
				yield return "/OPT:REF";
				yield return "/OPT:ICF";
				if (base.BuildConfiguration == BuildConfiguration.ReleasePlus || base.BuildConfiguration == BuildConfiguration.ReleaseSize)
				{
					yield return "/LTCG";
				}
				if (base.Architecture is ARMv7Architecture)
				{
					yield return "/OPT:LBR";
				}
			}
			if (DontLinkCrt)
			{
				yield return "/ENTRY:wWinMain";
			}
			if (base.Architecture is x64Architecture)
			{
				yield return "/HIGHENTROPYVA";
			}
			if (outputFile.HasExtension(DynamicLibraryExtension))
			{
				yield return "/DLL";
			}
			yield return "/IGNORE:4104";
			if (_treatWarningsAsErrors)
			{
				yield return "/WX";
			}
			yield return "/NODEFAULTLIB:uuid.lib";
			if (DontLinkCrt)
			{
				yield return "/NODEFAULTLIB";
				if (base.Architecture is x86Architecture)
				{
					yield return "int64.lib";
				}
			}
			foreach (string item in ToolChainStaticLibraries())
			{
				yield return item.InQuotes();
			}
			foreach (NPath staticLibrary in staticLibraries)
			{
				yield return staticLibrary.InQuotes();
			}
			foreach (NPath dynamicLibrary in dynamicLibraries)
			{
				NPath nPath = dynamicLibrary.ChangeExtension(".lib");
				if (nPath.Exists())
				{
					yield return nPath.InQuotes();
					continue;
				}
				nPath = dynamicLibrary.ChangeExtension(".dll.lib");
				if (nPath.Exists())
				{
					yield return nPath.InQuotes();
				}
			}
			foreach (string item2 in ToolChainLibraryPaths().InQuotes().PrefixedWith("/LIBPATH:"))
			{
				yield return item2;
			}
		}

		public override bool DynamicLibrariesHaveToSitNextToExecutable()
		{
			return true;
		}

		public override IEnumerable<NPath> ToolChainIncludePaths()
		{
			return MsvcInstallation.GetIncludeDirectories(base.Architecture);
		}

		public override IEnumerable<NPath> ToolChainLibraryPaths()
		{
			return MsvcInstallation.GetLibDirectories(base.Architecture);
		}

		public virtual IEnumerable<NPath> ToolChainPlatformMetadataReferences()
		{
			return MsvcInstallation.GetPlatformMetadataReferences();
		}

		public virtual IEnumerable<NPath> ToolChainWindowsMetadataReferences()
		{
			return MsvcInstallation.GetWindowsMetadataReferences();
		}

		public override NPath CompilerExecutableFor(NPath sourceFile)
		{
			if (IsAsmFile(sourceFile))
			{
				if (base.Architecture is x64Architecture)
				{
					return MsvcInstallation.GetVSToolPath(base.Architecture, "ml64.exe");
				}
				if (base.Architecture is x86Architecture)
				{
					return MsvcInstallation.GetVSToolPath(base.Architecture, "ml.exe");
				}
				throw new NotImplementedException("Cannot compile assembly file for architecture " + base.Architecture.Name);
			}
			return CompilerPath;
		}

		public override IEnumerable<string> CompilerFlagsFor(CppCompilationInstruction cppCompilationInstruction)
		{
			if (IsAsmFile(cppCompilationInstruction.SourceFile))
			{
				yield return "/c";
				yield break;
			}
			yield return cppCompilationInstruction.SourceFile.InQuotes();
			foreach (string item in ChooseCompilerFlags(cppCompilationInstruction, DefaultCompilerFlags))
			{
				yield return item;
			}
			foreach (string define in cppCompilationInstruction.Defines)
			{
				yield return "/D" + define;
			}
			foreach (NPath includePath in cppCompilationInstruction.IncludePaths)
			{
				yield return string.Concat("/I\"", includePath, "\"");
			}
		}

		protected virtual IEnumerable<string> DefaultCompilerFlags(CppCompilationInstruction cppCompilationInstruction)
		{
			bool hasClrFlag = cppCompilationInstruction.CompilerFlags.Any((string flag) => flag.ToLower().StartsWith("/clr"));
			yield return "/nologo";
			yield return "/c";
			yield return "/bigobj";
			yield return "/W3";
			yield return "/Z7";
			if (!hasClrFlag)
			{
				if (!_disableExceptions && !DontLinkCrt)
				{
					yield return "/EHs";
				}
				yield return "/GR-";
			}
			yield return "/Gy";
			yield return "/utf-8";
			yield return "/wd4102";
			yield return "/wd4800";
			yield return "/wd4056";
			yield return "/wd4190";
			yield return "/wd4723";
			yield return "/wd4467";
			yield return "/wd4503";
			yield return "/wd4996";
			yield return "/wd4200";
			yield return "/wd4834";
			if (cppCompilationInstruction.TreatWarningsAsErrors && _treatWarningsAsErrors)
			{
				yield return "/WX";
			}
			if (base.BuildConfiguration == BuildConfiguration.Debug)
			{
				yield return "/Od";
				yield return "/Zc:inline";
				if (!hasClrFlag && !DontLinkCrt)
				{
					yield return "/RTC1";
				}
				if (DontLinkCrt)
				{
					yield return "/GS-";
				}
			}
			else
			{
				if (base.BuildConfiguration == BuildConfiguration.ReleaseSize)
				{
					yield return "/O1";
				}
				else
				{
					yield return "/Ox";
				}
				yield return "/Oi";
				yield return "/Oy-";
				yield return "/GS-";
				yield return "/Gw";
				yield return "/GF";
				yield return "/Zo";
				if ((base.BuildConfiguration == BuildConfiguration.ReleasePlus || base.BuildConfiguration == BuildConfiguration.ReleaseSize) && (!DontLinkCrt || !string.Equals(cppCompilationInstruction.SourceFile.FileName, "crt.cpp", StringComparison.OrdinalIgnoreCase)))
				{
					yield return "/GL";
				}
			}
			if (DontLinkCrt && base.Architecture is x86Architecture)
			{
				yield return "/arch:SSE";
			}
			if (string.Equals(cppCompilationInstruction.SourceFile.FileName, _showIncludesForFile, StringComparison.OrdinalIgnoreCase))
			{
				yield return "/showIncludes";
			}
			if (HasPrecompiledHeader(cppCompilationInstruction.SourceFile))
			{
				if (_pchCHeaderFile != null && cppCompilationInstruction.SourceFile.ExtensionWithDot.Equals(".c"))
				{
					yield return "/Yu" + _pchCHeaderFile.ChangeExtension(".h").FileName;
					yield return "/Fp" + _pchCObjectFile.ChangeExtension(".pch").InQuotes();
				}
				if (_pchCppHeaderFile != null && cppCompilationInstruction.SourceFile.ExtensionWithDot.Equals(".cpp"))
				{
					yield return "/Yu" + _pchCppHeaderFile.ChangeExtension(".hpp").FileName;
					yield return "/Fp" + _pchCppObjectFile.ChangeExtension(".pch").InQuotes();
				}
			}
		}

		private bool HasPrecompiledHeader(NPath sourceFile)
		{
			if (sourceFile.FileName.Equals("pch-c.c") || sourceFile.FileName.Equals("pch-cpp.cpp"))
			{
				return false;
			}
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

		private IEnumerable<string> IdlCompilerArgumentsFor(NPath idl)
		{
			yield return "/W3";
			yield return "/nologo";
			yield return "/ns_prefix";
			yield return "/char unsigned";
			if (base.Architecture is x86Architecture)
			{
				yield return "/env win32";
			}
			else if (base.Architecture is x64Architecture)
			{
				yield return "/env x64";
			}
			else if (base.Architecture is ARMv7Architecture)
			{
				yield return "/env arm32";
			}
			else if (base.Architecture is ARM64Architecture)
			{
				yield return "/env arm64";
			}
			foreach (NPath item in ToolChainIncludePaths())
			{
				yield return "/I " + item.InQuotes();
			}
			if (MsvcInstallation.HasMetadataDirectories())
			{
				yield return "/winrt";
				yield return $"/metadata_dir \"{MsvcInstallation.GetUnionMetadataDirectory()}\"";
			}
			foreach (string item2 in ToolChainDefines())
			{
				yield return "/D " + item2;
			}
			yield return "/Oicf";
			yield return $"/h \"{idl.FileNameWithoutExtension}_h.h\"";
			yield return idl.InQuotes();
		}

		private IEnumerable<string> IdlOutputArgumentsFor(NPath outputDir, NPath outputTlbPath, NPath outputWinmdPath)
		{
			yield return $"/out{outputDir.InQuotes()}";
			yield return $"/tlb {outputTlbPath.InQuotes()}";
			yield return $"/winmd {outputWinmdPath.InQuotes()}";
		}

		private IEnumerable<string> ManifestCompilerArgumentsFor(NPath tlbFile, NPath outputFile)
		{
			yield return "/nologo";
			yield return "/verbose";
			yield return $"/identity:\"{outputFile.FileNameWithoutExtension},type=win32,version=1.0.0.0\"";
			yield return $"/tlb:{tlbFile.InQuotes()}";
			yield return $"/dll:{outputFile.FileName.InQuotes()}";
		}

		private string ManifestOutputArgumentFor(NPath path)
		{
			return $"/out:{path.InQuotes()}";
		}

		public override IEnumerable<Type> SupportedArchitectures()
		{
			return MsvcInstallation.SupportedArchitectures;
		}

		public static IEnumerable<string> FlagsToMakeWarningsErrorsFor(string sourceFile)
		{
			yield return "/WX";
		}

		public override void OnBeforeCompile(ProgramBuildDescription programBuildDescription, CppToolChainContext toolChainContext, HeaderFileHashProvider headerHashProvider, NPath workingDirectory, bool forceRebuild, bool verbose, bool includeFileNamesInHashes)
		{
			base.OnBeforeCompile(programBuildDescription, toolChainContext, headerHashProvider, workingDirectory, forceRebuild, verbose, includeFileNamesInHashes);
			ParallelFor.Run(new Action[2]
			{
				delegate
				{
					CompilePch(programBuildDescription, headerHashProvider, workingDirectory, forceRebuild, verbose, includeFileNamesInHashes);
				},
				delegate
				{
					CompileIDL(programBuildDescription, headerHashProvider, toolChainContext, workingDirectory, forceRebuild, verbose);
					CompileCOMManifest(programBuildDescription, toolChainContext, workingDirectory, forceRebuild, verbose);
				}
			}, delegate(Action f)
			{
				f();
			});
			FindModuleDefinitionFiles(programBuildDescription, toolChainContext);
		}

		private void CompilePch(ProgramBuildDescription programBuildDescription, HeaderFileHashProvider headerHashProvider, NPath workingDirectory, bool forceRebuild, bool verbose, bool includeFileNamesInHashes)
		{
			IL2CPPOutputBuildDescription il2CppOutputBuildDescription = programBuildDescription as IL2CPPOutputBuildDescription;
			if (il2CppOutputBuildDescription != null)
			{
				ParallelFor.Run(new Action[2]
				{
					delegate
					{
						InvokePchCompilation(il2CppOutputBuildDescription, headerHashProvider, workingDirectory, forceRebuild, verbose, includeFileNamesInHashes, il2CppOutputBuildDescription.PchCSourceFile);
					},
					delegate
					{
						InvokePchCompilation(il2CppOutputBuildDescription, headerHashProvider, workingDirectory, forceRebuild, verbose, includeFileNamesInHashes, il2CppOutputBuildDescription.PchCppSourceFile);
					}
				}, delegate(Action f)
				{
					f();
				});
			}
		}

		private void InvokePchCompilation(IL2CPPOutputBuildDescription programBuildDescription, HeaderFileHashProvider headerHashProvider, NPath workingDirectory, bool forceRebuild, bool verbose, bool includeFileNamesInHashes, NPath sourceFile)
		{
			if (sourceFile == null || !sourceFile.Exists())
			{
				return;
			}
			string text = (sourceFile.ExtensionWithDot.Equals(".c") ? sourceFile.ChangeExtension(".h").FileName : sourceFile.ChangeExtension(".hpp").FileName);
			IEnumerable<NPath> enumerable = ToolChainIncludePaths().Concat(programBuildDescription.AdditionalIncludePathsFor(sourceFile));
			NPath[] array = (enumerable as NPath[]) ?? enumerable.ToArray();
			CppCompilationInstruction cppCompilationInstruction = new CppCompilationInstruction
			{
				SourceFile = sourceFile,
				Defines = ToolChainDefines().Concat(programBuildDescription.AdditionalDefinesFor(sourceFile)),
				IncludePaths = array,
				LumpPaths = Enumerable.Empty<NPath>(),
				CompilerFlags = programBuildDescription.AdditionalCompilerFlags,
				CacheDirectory = workingDirectory,
				TreatWarningsAsErrors = _treatWarningsAsErrors
			};
			CompilationInvocation compilationInvocation = new CompilationInvocation
			{
				CompilerExecutable = CompilerPath,
				SourceFile = sourceFile,
				EnvVars = EnvVars(),
				Arguments = CompilerFlagsFor(cppCompilationInstruction).Concat(programBuildDescription.CompilerFlagsFor(cppCompilationInstruction)).Append("/Yc" + text)
			};
			string hashForAllHeaderFilesPossiblyInfluencingCompilation = headerHashProvider.HashForAllIncludableFilesInDirectories(array, new string[2] { ".h", ".hpp" });
			string text2 = compilationInvocation.Hash(hashForAllHeaderFilesPossiblyInfluencingCompilation);
			NPath nPath = workingDirectory.Combine(text2).ChangeExtension(ObjectExtension());
			if (includeFileNamesInHashes)
			{
				string text3 = cppCompilationInstruction.SourceFile.FileName.Replace('.', '_');
				nPath = workingDirectory.Combine(text3 + text2).ChangeExtension(ObjectExtension());
			}
			compilationInvocation.Arguments = compilationInvocation.Arguments.Append("/Fp" + nPath.ChangeExtension(".pch").InQuotes()).Concat(OutputArgumentFor(nPath, sourceFile));
			if (sourceFile.ExtensionWithDot.Equals(".c"))
			{
				_pchCHeaderFile = sourceFile.ChangeExtension(".h");
				_pchCObjectFile = nPath;
				if (!forceRebuild && _pchCHeaderFile.Exists() && _pchCObjectFile.Exists())
				{
					return;
				}
			}
			else if (sourceFile.ExtensionWithDot.Equals(".cpp"))
			{
				_pchCppHeaderFile = sourceFile.ChangeExtension(".hpp");
				_pchCppObjectFile = nPath;
				if (!forceRebuild && _pchCppHeaderFile.Exists() && _pchCppObjectFile.Exists())
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

		private void CompileIDL(ProgramBuildDescription programBuildDescription, HeaderFileHashProvider headerHashProvider, CppToolChainContext toolChainContext, NPath workingDirectory, bool forceRebuild, bool verbose)
		{
			if (!programBuildDescription.OutputFile.HasExtension(".dll") || !(programBuildDescription is IHaveSourceDirectories haveSourceDirectories))
			{
				return;
			}
			NPath[] array = haveSourceDirectories.SourceDirectories.SelectMany((NPath d) => d.Files("*.idl")).ToArray();
			if (array.Length == 0)
			{
				return;
			}
			NPath idlResultDirectory = workingDirectory.EnsureDirectoryExists("idlGenerated");
			IEnumerable<NPath> includePaths = ToolChainIncludePaths();
			IEnumerable<IdlCompilationResult> enumerable = ParallelFor.RunWithResult(array, delegate(NPath idl)
			{
				string hashForAllHeaderFilesPossiblyInfluencingCompilation = headerHashProvider.HashForAllIncludableFilesInDirectories(includePaths.Append(idl.Parent), new string[1] { ".idl" });
				CompilationInvocation compilationInvocation = new CompilationInvocation
				{
					CompilerExecutable = MsvcInstallation.GetSDKToolPath("midl.exe"),
					SourceFile = idl,
					EnvVars = EnvVars(),
					Arguments = IdlCompilerArgumentsFor(idl)
				};
				NPath nPath = idlResultDirectory.Combine(compilationInvocation.Hash(hashForAllHeaderFilesPossiblyInfluencingCompilation));
				if (nPath.DirectoryExists() && nPath.Files().Count() > 0 && !forceRebuild)
				{
					return IdlCompilationResult.SuccessfulResult(nPath);
				}
				nPath.EnsureDirectoryExists();
				NPath outputTlbPath = nPath.Combine(programBuildDescription.OutputFile.ChangeExtension(".tlb").FileName);
				NPath outputWinmdPath = nPath.Combine(programBuildDescription.OutputFile.ChangeExtension(".winmd").FileName);
				compilationInvocation.Arguments = compilationInvocation.Arguments.Concat(IdlOutputArgumentsFor(nPath, outputTlbPath, outputWinmdPath));
				Shell.ExecuteResult shellExecuteResult;
				using (MiniProfiler.Section("Compile IDL", idl.ToString()))
				{
					shellExecuteResult = compilationInvocation.Execute();
				}
				IdlCompilationResult idlCompilationResult = IdlCompilationResult.FromShellExecuteResult(shellExecuteResult, compilationInvocation, nPath);
				if (idlCompilationResult.Success && verbose)
				{
					ConsoleOutput.Info.WriteLine(idlCompilationResult.StdOut.Trim());
				}
				return idlCompilationResult;
			});
			MsvcToolChainContext msvcToolChainContext = (MsvcToolChainContext)toolChainContext;
			foreach (IdlCompilationResult item in enumerable)
			{
				if (!item.Success)
				{
					throw new BuilderFailedException(string.Format(item.StdOut + "{0}{0}Invocation was: " + item.Invocation.Summary(), Environment.NewLine));
				}
				msvcToolChainContext.AddCompileInstructions(from sourceFile in item.OutputDirectory.Files("*_i.c")
					select new CppCompilationInstruction
					{
						SourceFile = sourceFile,
						CompilerFlags = programBuildDescription.AdditionalCompilerFlags,
						IncludePaths = programBuildDescription.AdditionalIncludePathsFor(sourceFile),
						Defines = programBuildDescription.AdditionalDefinesFor(sourceFile)
					});
				msvcToolChainContext.AddIncludeDirectory(item.OutputDirectory);
				foreach (NPath item2 in item.OutputDirectory.Files("*.tlb"))
				{
					item2.Copy(programBuildDescription.OutputFile.Parent.Combine(item2.FileName));
				}
				foreach (NPath item3 in item.OutputDirectory.Files("*.winmd"))
				{
					item3.Copy(programBuildDescription.OutputFile.Parent.Combine(item3.FileName));
				}
			}
		}

		private void CompileCOMManifest(ProgramBuildDescription programBuildDescription, CppToolChainContext toolChainContext, NPath workingDirectory, bool forceRebuild, bool verbose)
		{
			if (!programBuildDescription.OutputFile.HasExtension(".dll"))
			{
				return;
			}
			NPath nPath = programBuildDescription.OutputFile.ChangeExtension(".tlb");
			if (!nPath.Exists())
			{
				return;
			}
			string hashForAllHeaderFilesPossiblyInfluencingCompilation = HashTools.HashOfFile(nPath);
			CompilationInvocation compilationInvocation = new CompilationInvocation
			{
				CompilerExecutable = MsvcInstallation.GetSDKToolPath("mt.exe"),
				SourceFile = nPath,
				Arguments = ManifestCompilerArgumentsFor(nPath, programBuildDescription.OutputFile)
			};
			NPath nPath2 = workingDirectory.Combine(compilationInvocation.Hash(hashForAllHeaderFilesPossiblyInfluencingCompilation) + ".manifest");
			((MsvcToolChainContext)toolChainContext).ManifestPath = nPath2;
			if (!nPath2.Exists() || forceRebuild)
			{
				compilationInvocation.Arguments = compilationInvocation.Arguments.Append(ManifestOutputArgumentFor(nPath2));
				Shell.ExecuteResult executeResult;
				using (MiniProfiler.Section("Compile manifest", nPath2.ToString()))
				{
					executeResult = compilationInvocation.Execute();
				}
				if (executeResult.ExitCode != 0)
				{
					throw new BuilderFailedException(string.Format(executeResult.StdOut + "{0}{0}Invocation was: " + compilationInvocation.Summary(), Environment.NewLine));
				}
				if (verbose)
				{
					ConsoleOutput.Info.WriteLine(executeResult.StdOut.Trim());
				}
			}
		}

		private void FindModuleDefinitionFiles(ProgramBuildDescription programBuildDescription, CppToolChainContext toolChainContext)
		{
			if (!programBuildDescription.OutputFile.HasExtension(".dll") || !(programBuildDescription is IHaveSourceDirectories haveSourceDirectories))
			{
				return;
			}
			NPath[] array = haveSourceDirectories.SourceDirectories.SelectMany((NPath d) => d.Files("*.def")).ToArray();
			if (array.Length == 0)
			{
				return;
			}
			if (array.Length > 1)
			{
				throw new BuilderFailedException(string.Format("Found more than one module definition file in source directories:{0}\t{1}", Environment.NewLine, array.Select((NPath f) => f.ToString()).Aggregate((string x, string y) => x + Environment.NewLine + "\t" + y)));
			}
			((MsvcToolChainContext)toolChainContext).ModuleDefinitionPath = array.Single();
		}

		private static NPath PickBestDirectoryToUseAsWorkingDirectoryFromObjectFilePaths(IEnumerable<NPath> objectFiles)
		{
			Dictionary<NPath, int> dictionary = new Dictionary<NPath, int>();
			foreach (NPath objectFile in objectFiles)
			{
				if (!objectFile.IsRelative)
				{
					if (!dictionary.TryGetValue(objectFile.Parent, out var _))
					{
						dictionary.Add(objectFile.Parent, 1);
					}
					else
					{
						dictionary[objectFile.Parent]++;
					}
				}
			}
			int num = int.MinValue;
			NPath result = null;
			foreach (KeyValuePair<NPath, int> item in dictionary)
			{
				int num2 = item.Value & item.Key.ToString().Length;
				if (num2 > num)
				{
					num = num2;
					result = item.Key;
				}
			}
			return result;
		}

		public override string GetToolchainInfoForOutput()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine(base.GetToolchainInfoForOutput());
			stringBuilder.AppendLine($"\tMsvc Install Version: {MsvcInstallation.Version}");
			stringBuilder.AppendLine($"\tMsvc Install SDK Directory: {MsvcInstallation.SDKDirectory}");
			stringBuilder.AppendLine($"\tMsvc Linker Path: {LinkerPath}");
			stringBuilder.AppendLine($"\tMsvc Compiler Path: {CompilerPath}");
			return stringBuilder.ToString();
		}

		protected override string GetInterestingOutputFromCompilationShellResult(Shell.ExecuteResult shellResult)
		{
			return shellResult.StdOut.Trim() + Environment.NewLine + shellResult.StdErr.Trim();
		}

		protected override string GetInterestingOutputFromLinkerShellResult(Shell.ExecuteResult shellResult)
		{
			return shellResult.StdOut.Trim() + Environment.NewLine + shellResult.StdErr.Trim();
		}

		public override bool CanGenerateAssemblyCode()
		{
			return true;
		}

		public override SourceCodeSearcher SourceCodeSearcher()
		{
			return new MsvcToolChainAssemblyCodeSearcher();
		}
	}
}
