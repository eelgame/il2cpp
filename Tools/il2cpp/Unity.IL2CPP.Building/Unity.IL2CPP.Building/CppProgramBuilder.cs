using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using NiceIO;
using Unity.IL2CPP.Building.BuildDescriptions;
using Unity.IL2CPP.Building.Hashing;
using Unity.IL2CPP.Building.Platforms;
using Unity.IL2CPP.Building.Statistics;
using Unity.IL2CPP.Building.ToolChains;
using Unity.IL2CPP.Common;
using Unity.MiniProfiling;

namespace Unity.IL2CPP.Building
{
	public sealed class CppProgramBuilder
	{
		public class LinkerInvocation
		{
			public Shell.ExecuteArgs ExecuteArgs;

			public List<string> ArgumentsInfluencingOutcome;

			public IEnumerable<NPath> FilesInfluencingOutcome;
		}

		private readonly ProgramBuildDescription _programBuildDescription;

		private readonly CppToolChain _cppToolChain;

		private readonly NPath _globalObjectCacheDirectory;

		private readonly NPath _workingDirectory;

		private readonly HeaderFileHashProvider _headerHashProvider;

		private readonly LumpHashProvider _lumpedHashProvider;

		private readonly bool _verbose;

		private readonly bool _forceRebuild;

		private readonly bool _includeFileNamesInHashes;

		private readonly bool _avoidDynamicLibraryCopy;

		private readonly ReadOnlyCollection<NPath> _sourceFilesWithAssemblyOutput;

		public static CppProgramBuilder Create(RuntimePlatform platform, ProgramBuildDescription programBuildDescription, bool verbose, Architecture architecture, BuildConfiguration buildConfiguration, bool forceRebuild, bool treatWarningsAsErrors, bool includeFileNamesInHashes, bool assemblyOutput, string toolChainPath)
		{
			PlatformSupport platformSupport = PlatformSupport.For(platform);
			return new CppProgramBuilder(CppToolChainFor(platform, architecture, buildConfiguration, treatWarningsAsErrors, assemblyOutput, toolChainPath), platformSupport.PostProcessProgramBuildDescription(programBuildDescription), verbose, forceRebuild, includeFileNamesInHashes);
		}

		public static CppToolChain CppToolChainFor(RuntimePlatform platform, Architecture architecture, BuildConfiguration buildConfiguration, bool treatWarningsAsErrors, bool assemblyOutput, string toolChainPath)
		{
			return CppToolChainFor(platform, architecture, buildConfiguration, treatWarningsAsErrors, assemblyOutput, toolChainPath, null);
		}

		public static CppToolChain CppToolChainFor(RuntimePlatform platform, Architecture architecture, BuildConfiguration buildConfiguration, bool treatWarningsAsErrors, bool assemblyOutput, string toolChainPath, string sysrootPath)
		{
			PlatformSupport platformSupport = PlatformSupport.For(platform);
			BuildingOptions buildingOptions = new BuildingOptions
			{
				Architecture = architecture,
				Configuration = buildConfiguration,
				TreatWarningsAsErrors = treatWarningsAsErrors,
				AssemblyOutput = assemblyOutput,
				UseDependenciesToolChain = false
			};
			if (!string.IsNullOrEmpty(toolChainPath))
			{
				buildingOptions.ToolChainPath = toolChainPath;
			}
			if (!string.IsNullOrEmpty(sysrootPath))
			{
				buildingOptions.SysrootPath = sysrootPath;
			}
			return platformSupport.MakeCppToolChain(buildingOptions);
		}

		public CppProgramBuilder(CppToolChain cppToolChain, ProgramBuildDescription programBuildDescription, bool verbose, bool forceRebuild, bool includeFileNamesInHashes)
			: this(cppToolChain, programBuildDescription, verbose, forceRebuild, includeFileNamesInHashes, avoidDynamicLibraryCopy: false, null)
		{
		}

		public CppProgramBuilder(CppToolChain cppToolChain, ProgramBuildDescription programBuildDescription, HeaderFileHashProvider headerHashProvider, bool verbose, bool forceRebuild, bool includeFileNamesInHashes)
			: this(cppToolChain, programBuildDescription, headerHashProvider, verbose, forceRebuild, includeFileNamesInHashes, avoidDynamicLibraryCopy: false, null)
		{
		}

		public CppProgramBuilder(CppToolChain cppToolChain, ProgramBuildDescription programBuildDescription, bool verbose, bool forceRebuild, bool includeFileNamesInHashes, bool avoidDynamicLibraryCopy, ReadOnlyCollection<NPath> sourceFilesWithAssemblyOutput)
			: this(cppToolChain, programBuildDescription, new HeaderFileHashProvider(), verbose, forceRebuild, includeFileNamesInHashes, avoidDynamicLibraryCopy, sourceFilesWithAssemblyOutput)
		{
		}

		public CppProgramBuilder(CppToolChain cppToolChain, ProgramBuildDescription programBuildDescription, HeaderFileHashProvider headerHashProvider, bool verbose, bool forceRebuild, bool includeFileNamesInHashes, bool avoidDynamicLibraryCopy, ReadOnlyCollection<NPath> sourceFilesWithAssemblyOutput)
		{
			_verbose = verbose;
			_forceRebuild = forceRebuild;
			_avoidDynamicLibraryCopy = avoidDynamicLibraryCopy;
			_programBuildDescription = programBuildDescription;
			_cppToolChain = cppToolChain;
			_includeFileNamesInHashes = includeFileNamesInHashes;
			_sourceFilesWithAssemblyOutput = sourceFilesWithAssemblyOutput ?? new List<NPath>().AsReadOnly();
			_workingDirectory = programBuildDescription.GlobalCacheDirectory ?? TempDir.Empty("workingdir_" + programBuildDescription.GetType().Name);
			_globalObjectCacheDirectory = _workingDirectory.EnsureDirectoryExists("globalcache");
			_headerHashProvider = headerHashProvider;
			_lumpedHashProvider = new LumpHashProvider();
		}

		public NPath Build()
		{
			IBuildStatistics statistics;
			return Build(out statistics);
		}

		public NPath Build(out IBuildStatistics statistics)
		{
			ThrowIfCannotBuildInCurrentEnvironment();
			TraceDebugger.Flush();
			CppProgramBuildStatistics cppProgramBuildStatistics = new CppProgramBuildStatistics();
			string text = string.Format("Building {0} with {1}{2}\tOutput directory: {3}{2}\tCache directory: {4}", _programBuildDescription.OutputFile.FileName, _cppToolChain.GetToolchainInfoForOutput(), Environment.NewLine, _programBuildDescription.OutputFile.Parent, _workingDirectory);
			ConsoleOutput.Info.WriteLine(text);
			TraceDebugger.Log(text);
			using (MiniProfiler.Section("BuildBinary"))
			{
				CppToolChainContext cppToolChainContext = _cppToolChain.CreateToolChainContext();
				IEnumerable<NPath> enumerable = _cppToolChain.PrecompiledHeaderObjectFiles();
				CppCompilationInstruction[] array;
				using (MiniProfiler.Section("FindFilesToCompile"))
				{
					array = _programBuildDescription.CppCompileInstructions.Concat(cppToolChainContext.ExtraCompileInstructions).ToArray();
					CppCompilationInstruction[] array2 = array;
					foreach (CppCompilationInstruction obj in array2)
					{
						obj.Defines = obj.Defines.Concat(_cppToolChain.ToolChainDefines());
						obj.IncludePaths = obj.IncludePaths.Concat(_cppToolChain.ToolChainIncludePaths()).Concat(cppToolChainContext.ExtraIncludeDirectories);
					}
				}
				using (MiniProfiler.Section("Calculate header hashes"))
				{
					_headerHashProvider.Initialize(array, new string[2] { ".h", ".inc" });
				}
				using (MiniProfiler.Section("Calculate lumped hashes"))
				{
					_lumpedHashProvider.Initialize(array, new string[2] { ".cpp", ".c" });
				}
				using (MiniProfiler.Section("ToolChain OnBeforeCompile Build"))
				{
					_cppToolChain.OnBeforeCompile(_programBuildDescription, cppToolChainContext, _headerHashProvider, _workingDirectory, _forceRebuild, _verbose, _includeFileNamesInHashes);
				}
				HashSet<NPath> hashSet;
				using (MiniProfiler.Section("BuildAllCppFiles"))
				{
					hashSet = BuildAllCppFiles(array, cppProgramBuildStatistics);
				}
				if (enumerable != null)
				{
					hashSet.UnionWith(enumerable);
				}
				if (!_sourceFilesWithAssemblyOutput.Any())
				{
					using (MiniProfiler.Section("OnBeforeLink Build"))
					{
						OnBeforeLink(hashSet, cppToolChainContext);
					}
					using (MiniProfiler.Section("Postprocess Object Files"))
					{
						PostprocessObjectFiles(hashSet, cppToolChainContext);
					}
					using (MiniProfiler.Section("ProgramDescription Finalize Build"))
					{
						_programBuildDescription.FinalizeBuild(_cppToolChain);
					}
					using (MiniProfiler.Section("Clean IL2CPP Cache"))
					{
						CleanWorkingDirectory(hashSet);
					}
				}
			}
			statistics = cppProgramBuildStatistics;
			return _programBuildDescription.OutputFile;
		}

		private void OnBeforeLink(HashSet<NPath> objectFiles, CppToolChainContext toolChainContext)
		{
			using (MiniProfiler.Section("ToolChain OnBeforeLink Build"))
			{
				_cppToolChain.OnBeforeLink(_programBuildDescription, _workingDirectory, objectFiles, toolChainContext, _forceRebuild, _verbose);
			}
			using (MiniProfiler.Section("ProgramBuildDescription OnBeforeLink Build"))
			{
				_programBuildDescription.OnBeforeLink(_headerHashProvider, _workingDirectory, objectFiles, toolChainContext, _forceRebuild, _verbose, _includeFileNamesInHashes);
			}
		}

		private void CleanWorkingDirectory(HashSet<NPath> compiledObjectFiles)
		{
			IEnumerable<NPath> source = compiledObjectFiles.Select((NPath file) => file.Parent).Distinct().SelectMany((NPath d) => d.Files());
			NPath[] array = source.Where((NPath objectFile) => compiledObjectFiles.Any((NPath compiledObjectFile) => string.Equals(objectFile.FileNameWithoutExtension, compiledObjectFile.FileNameWithoutExtension, StringComparison.OrdinalIgnoreCase))).ToArray();
			for (int i = 0; i < array.Length; i++)
			{
				File.SetLastAccessTimeUtc(array[i].ToString(), DateTime.UtcNow);
			}
			NPath[] array2 = source.Where((NPath objectFile) => !compiledObjectFiles.Any((NPath compiledObjectFile) => string.Equals(objectFile.FileNameWithoutExtension, compiledObjectFile.FileNameWithoutExtension, StringComparison.OrdinalIgnoreCase))).ToArray();
			array = array2;
			foreach (NPath nPath in array)
			{
				try
				{
					nPath.Delete();
				}
				catch (IOException)
				{
				}
			}
			if (_programBuildDescription.GlobalCacheDirectory == null)
			{
				_workingDirectory.Delete(DeleteMode.Soft);
			}
			ConsoleOutput.Info.WriteLine("Cleaned up {0} object files.", array2.Length);
		}

		private HashSet<NPath> BuildAllCppFiles(IEnumerable<CppCompilationInstruction> sourceFilesToCompile, IBuildStatisticsCollector statisticsCollector)
		{
			using (MiniProfiler.Section("Compile"))
			{
				Stopwatch stopwatch = new Stopwatch();
				stopwatch.Start();
				bool flag = _sourceFilesWithAssemblyOutput.Count > 0;
				if (flag)
				{
					sourceFilesToCompile = sourceFilesToCompile.Where((CppCompilationInstruction ci) => _sourceFilesWithAssemblyOutput.Contains(ci.SourceFile));
				}
				CppCompilationInstruction[] array = sourceFilesToCompile.OrderByDescending((CppCompilationInstruction f) => new FileInfo(f.SourceFile.ToString()).Length).ToArray();
				statisticsCollector.IncrementTotalFileCountBy(array.Length);
				List<IntermediateObjectFileCompilationData> list = ParallelFor.RunWithResult(array, BuildIntermediateObjectFileData).ToList();
				List<IntermediateObjectFileCompilationData> list2 = new List<IntermediateObjectFileCompilationData>();
				List<IntermediateObjectFileCompilationData> list3 = new List<IntermediateObjectFileCompilationData>();
				foreach (IntermediateObjectFileCompilationData item in list)
				{
					if (IsCached(item) && !flag)
					{
						list2.Add(item);
					}
					else
					{
						list3.Add(item);
					}
				}
				statisticsCollector.IncrementCacheHitCountBy(list2.Count);
				List<ProvideObjectResult> list4 = new List<ProvideObjectResult>();
				List<CompilationResult> list5 = new List<CompilationResult>();
				list4.AddRange(list2.Select((IntermediateObjectFileCompilationData d) => new ProvideObjectResult
				{
					ObjectFile = d.ObjectFile
				}));
				if (list3.Count > 0)
				{
					if (!_programBuildDescription.AllowCompilation())
					{
						throw new BuilderFailedException("Compilation has been blocked for testing purposes.   The following files needed to be compiled\n" + list3.Select((IntermediateObjectFileCompilationData n) => n.CppCompilationInstruction.SourceFile).AggregateWithNewLine());
					}
					foreach (NPath item2 in list3.Select((IntermediateObjectFileCompilationData c) => c.ObjectFile.Parent).Distinct())
					{
						item2.EnsureDirectoryExists();
					}
					List<ProvideObjectResult> list6 = ParallelFor.RunWithResult(list3.ToArray(), ProvideObjectFile).ToList();
					list4.AddRange(list6);
					list5.AddRange(list6.OfType<CompilationResult>());
					CompilationResult compilationResult = list5.FirstOrDefault((CompilationResult cr) => !cr.Success);
					if (compilationResult != null)
					{
						throw new BuilderFailedException(compilationResult.InterestingOutput + Environment.NewLine + "Invocation was: " + compilationResult.Invocation.Summary());
					}
				}
				ConsoleOutput.Info.WriteLine("ObjectFiles: " + list4.Count() + " of which compiled: " + list5.Count);
				foreach (CompilationResult item3 in list5.OrderByDescending((CompilationResult cr) => cr.Duration).Take(10))
				{
					ConsoleOutput.Info.WriteLine("\tTime Compile: {0} milliseconds {1}", item3.Duration.TotalMilliseconds, item3.Invocation.SourceFile.FileName);
				}
				ConsoleOutput.Info.WriteLine("Total compilation time: {0} milliseconds.", stopwatch.ElapsedMilliseconds);
				return new HashSet<NPath>(list4.Select((ProvideObjectResult c) => c.ObjectFile));
			}
		}

		private bool IsCached(IntermediateObjectFileCompilationData data)
		{
			if (data.CppCompilationInstruction.CacheDirectory != null && !_forceRebuild)
			{
				return data.ObjectFile.FileExists();
			}
			return false;
		}

		private IntermediateObjectFileCompilationData BuildIntermediateObjectFileData(CppCompilationInstruction cppCompilationInstruction)
		{
			CompilationInvocation compilationInvocation = new CompilationInvocation
			{
				CompilerExecutable = _cppToolChain.CompilerExecutableFor(cppCompilationInstruction.SourceFile),
				SourceFile = cppCompilationInstruction.SourceFile,
				Arguments = _cppToolChain.CompilerFlagsFor(cppCompilationInstruction).Concat(_programBuildDescription.CompilerFlagsFor(cppCompilationInstruction)),
				EnvVars = _cppToolChain.EnvVars()
			};
			string text2;
			using (MiniProfiler.Section("HashCompilerInvocation", cppCompilationInstruction.SourceFile.FileName))
			{
				string text = _headerHashProvider.HashForAllHeaderFilesReachableByFilesIn(cppCompilationInstruction, new string[2] { ".h", ".inc" });
				text2 = ((!cppCompilationInstruction.LumpPaths.Any()) ? compilationInvocation.Hash(text) : compilationInvocation.Hash(text + _lumpedHashProvider.HashForAllFilesUsedForLumping(cppCompilationInstruction, new string[2] { ".cpp", ".c" })));
			}
			NPath nPath = cppCompilationInstruction.CacheDirectory ?? _globalObjectCacheDirectory;
			NPath objectFile = nPath.Combine(text2).ChangeExtension(_cppToolChain.ObjectExtension());
			if (_includeFileNamesInHashes)
			{
				string text3 = cppCompilationInstruction.SourceFile.FileName.Replace('.', '_');
				objectFile = nPath.Combine(text3 + text2).ChangeExtension(_cppToolChain.ObjectExtension());
			}
			return new IntermediateObjectFileCompilationData
			{
				CppCompilationInstruction = cppCompilationInstruction,
				CompilationInvocation = compilationInvocation,
				ObjectFile = objectFile
			};
		}

		private ProvideObjectResult ProvideObjectFile(IntermediateObjectFileCompilationData data)
		{
			data.CompilationInvocation.Arguments = data.CompilationInvocation.Arguments.Concat(_cppToolChain.OutputArgumentFor(data.ObjectFile, data.CppCompilationInstruction.SourceFile));
			Shell.ExecuteResult executeResult;
			using (MiniProfiler.Section("Compile", data.CppCompilationInstruction.SourceFile.FileName))
			{
				executeResult = data.CompilationInvocation.Execute();
			}
			CompilationResult compilationResult = _cppToolChain.ShellResultToCompilationResult(executeResult);
			compilationResult.Invocation = data.CompilationInvocation;
			compilationResult.ObjectFile = data.ObjectFile;
			if (compilationResult.Success && _verbose && !string.IsNullOrWhiteSpace(executeResult.StdOut))
			{
				ConsoleOutput.Info.WriteLine(executeResult.StdOut.Trim());
			}
			return compilationResult;
		}

		private void PostprocessObjectFiles(HashSet<NPath> objectFiles, CppToolChainContext toolChainContext)
		{
			using (MiniProfiler.Section("Link", _programBuildDescription.OutputFile.FileName))
			{
				_programBuildDescription.OutputFile.EnsureParentDirectoryExists();
				LinkerInvocation linkerInvocation = _cppToolChain.MakeLinkerInvocation(objectFiles, _programBuildDescription.OutputFile, _programBuildDescription.GetStaticLibraries(_cppToolChain.BuildConfiguration), _programBuildDescription.GetDynamicLibraries(_cppToolChain.BuildConfiguration), _programBuildDescription.AdditionalLinkerFlags, toolChainContext);
				string text = HashLinkerInvocation(linkerInvocation, objectFiles);
				NPath nPath = _workingDirectory.Combine("linkresult_" + text);
				if (!_forceRebuild && nPath.DirectoryExists())
				{
					nPath.Files().Copy(_programBuildDescription.OutputFile.Parent);
				}
				else
				{
					Stopwatch stopwatch = new Stopwatch();
					stopwatch.Start();
					foreach (NPath item in _workingDirectory.Directories("linkresult_*"))
					{
						item.Delete();
					}
					nPath.EnsureDirectoryExists();
					NPath outputFile = nPath.Combine(_programBuildDescription.OutputFile.FileName);
					LinkerInvocation linkerInvocation2 = _cppToolChain.MakeLinkerInvocation(objectFiles, outputFile, _programBuildDescription.GetStaticLibraries(_cppToolChain.BuildConfiguration), _programBuildDescription.GetDynamicLibraries(_cppToolChain.BuildConfiguration), _programBuildDescription.AdditionalLinkerFlags, toolChainContext);
					Shell.ExecuteResult executeResult;
					using (MiniProfiler.Section("ActualLinkerInvocation", _programBuildDescription.OutputFile.FileName))
					{
						executeResult = BuildShell.Execute(linkerInvocation2.ExecuteArgs);
					}
					LinkerResult linkerResult = _cppToolChain.ShellResultToLinkerResult(executeResult);
					if (!linkerResult.Success)
					{
						nPath.DeleteIfExists();
						throw BuilderFailedExceptionForFailedLinkerExecution(linkerResult, linkerInvocation2.ExecuteArgs);
					}
					using (MiniProfiler.Section("ToolChain OnAfterLink Build"))
					{
						_cppToolChain.OnAfterLink(outputFile, toolChainContext, _forceRebuild, _verbose);
					}
					nPath.Files().Copy(_programBuildDescription.OutputFile.Parent);
					if (_verbose)
					{
						ConsoleOutput.Info.WriteLine(executeResult.StdOut.Trim());
					}
					ConsoleOutput.Info.WriteLine("Total link time: {0} milliseconds.", stopwatch.ElapsedMilliseconds);
					if (BuildShell.LogMode == BuildShell.CommandLogMode.DryRun && !nPath.Files().Any())
					{
						nPath.DeleteIfExists();
					}
				}
				if (_avoidDynamicLibraryCopy || !_cppToolChain.DynamicLibrariesHaveToSitNextToExecutable())
				{
					return;
				}
				foreach (NPath dynamicLibrary in _programBuildDescription.GetDynamicLibraries(_cppToolChain.BuildConfiguration))
				{
					if (!_programBuildDescription.OutputFile.Parent.Combine(dynamicLibrary.FileName).FileExists())
					{
						dynamicLibrary.Copy(_programBuildDescription.OutputFile.Parent);
					}
				}
			}
		}

		private string HashLinkerInvocation(LinkerInvocation linkerInvocation, HashSet<NPath> objectFiles)
		{
			using (MiniProfiler.Section("hash linker invocation"))
			{
				StringBuilder stringBuilder = new StringBuilder();
				foreach (string item in linkerInvocation.ArgumentsInfluencingOutcome)
				{
					stringBuilder.Append(item);
				}
				stringBuilder.Append(linkerInvocation.ExecuteArgs.Executable);
				foreach (NPath item2 in objectFiles.ToSortedCollection())
				{
					stringBuilder.Append(item2.FileName);
				}
				foreach (NPath item3 in from file in linkerInvocation.FilesInfluencingOutcome.ToSortedCollection()
					where !objectFiles.Contains(file)
					select file)
				{
					stringBuilder.Append(HashTools.HashOfFile(item3.IsRelative ? FindStaticLibrary(item3) : item3));
				}
				return HashTools.HashOf(stringBuilder.ToString());
			}
		}

		private NPath FindStaticLibrary(NPath staticLib)
		{
			try
			{
				return (from p in _cppToolChain.ToolChainLibraryPaths()
					select p.Combine(_cppToolChain.GetLibraryFileName(staticLib))).Single((NPath p) => p.FileExists());
			}
			catch
			{
				throw new UserMessageException($"Could not locate the exact path of {staticLib} inside these directories:{Environment.NewLine}\t{(from p in _cppToolChain.ToolChainLibraryPaths() select p.ToString()).Aggregate((string x, string y) => $"{x}{Environment.NewLine}\t{y}")}");
			}
		}

		private BuilderFailedException BuilderFailedExceptionForFailedLinkerExecution(LinkerResult result, Shell.ExecuteArgs executableInvocation)
		{
			return new BuilderFailedException(string.Format("{0} {1}{2}{2}{3}", executableInvocation.Executable, executableInvocation.Arguments, Environment.NewLine, result.InterestingOutput));
		}

		private void ThrowIfCannotBuildInCurrentEnvironment()
		{
			if (!_cppToolChain.CanBuildInCurrentEnvironment())
			{
				string text = _cppToolChain.GetCannotBuildInCurrentEnvironmentErrorMessage();
				if (string.IsNullOrEmpty(text))
				{
					text = $"Builder is unable to build using selected toolchain ({_cppToolChain.GetType().Name}) or architecture ({_cppToolChain.Architecture})!";
				}
				throw new UserMessageException(text);
			}
		}
	}
}
