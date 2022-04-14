using System;
using System.Collections.Generic;
using System.Linq;
using NiceIO;
using Unity.IL2CPP.Building.BuildDescriptions.Mono;
using Unity.IL2CPP.Building.Hashing;
using Unity.IL2CPP.Building.Platforms;
using Unity.IL2CPP.Building.Statistics;
using Unity.IL2CPP.Building.ToolChains;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Common.Profiles;
using Unity.IL2CPP.Options;
using Unity.MiniProfiling;

namespace Unity.IL2CPP.Building.BuildDescriptions
{
	public class IL2CPPOutputBuildDescription : ProgramBuildDescription
	{
		public class LibIL2CPPDynamicLibProgramDescription : ProgramBuildDescription
		{
			private readonly IEnumerable<CppCompilationInstruction> _cppCompileInstructions;

			private readonly NPath _cacheDirectory;

			public override IEnumerable<CppCompilationInstruction> CppCompileInstructions => _cppCompileInstructions;

			public override NPath GlobalCacheDirectory => _cacheDirectory;

			public LibIL2CPPDynamicLibProgramDescription(IEnumerable<CppCompilationInstruction> cppCompileInstructions, NPath outputFile, NPath cacheDirectory)
			{
				_cppCompileInstructions = cppCompileInstructions;
				_outputFile = outputFile;
				_cacheDirectory = cacheDirectory;
			}
		}

		public class MapFileParserBuildDescription : ProgramBuildDescription
		{
			private readonly NPath _cacheDirectory;

			public override IEnumerable<CppCompilationInstruction> CppCompileInstructions => from c in CommonPaths.Il2CppRoot.Combine("MapFileParser").Files("*.cpp", recurse: true)
				where !c.Elements.Contains("Tests")
				select c into f
				select new CppCompilationInstruction
				{
					SourceFile = f,
					CacheDirectory = _cacheDirectory,
					Defines = AdditionalDefinesFor(f)
				};

			public override NPath GlobalCacheDirectory => _cacheDirectory;

			public override NPath OutputFile => GlobalCacheDirectory.Combine("build").Combine("MapFileParser.exe");

			public MapFileParserBuildDescription(NPath cacheDirectory)
			{
				_cacheDirectory = cacheDirectory ?? TempDir.Empty("mapfileparser");
			}
		}

		public class MapFileParserUnitTestBuildDescription : MapFileParserBuildDescription
		{
			public override IEnumerable<CppCompilationInstruction> CppCompileInstructions
			{
				get
				{
					IEnumerable<CppCompilationInstruction> second = from c in CommonPaths.Il2CppRoot.Combine("MapFileParser").Files("*.cpp", recurse: true)
						where c.Elements.Contains("Tests")
						select c into f
						select new CppCompilationInstruction
						{
							SourceFile = f,
							CacheDirectory = GlobalCacheDirectory,
							Defines = AdditionalDefinesFor(f)
						};
					return base.CppCompileInstructions.Concat(second);
				}
			}

			public MapFileParserUnitTestBuildDescription(NPath cacheDirectory)
				: base(cacheDirectory)
			{
			}

			public override IEnumerable<string> AdditionalDefinesFor(NPath path)
			{
				return base.AdditionalDefinesFor(path).Concat(new string[1] { "ENABLE_UNIT_TESTS" });
			}
		}

		private readonly NPath _sourceDirectory;

		private readonly NPath _lumpedSourceDirectory;

		protected readonly NPath _cacheDirectory;

		protected readonly CppToolChain _cppToolChain;

		protected readonly PlatformSupport _platform;

		private readonly IEnumerable<NPath> _staticLibraries;

		private readonly IEnumerable<string> _additionalDefines;

		private readonly IEnumerable<NPath> _additionalIncludeDirectories;

		private readonly IEnumerable<string> _specifiedCompilerFlags;

		private readonly RuntimeGC _runtimeGC;

		private readonly MonoSourceFileList _monoSourceFileList;

		private NPath _dataFolder;

		private readonly string _relativeDataPath;

		private NPath _libil2cppCacheDirectory;

		private NPath _baselibDirectory;

		private NPath _mapFileParser;

		private readonly bool _disableRuntimeLumping;

		private readonly LumpFileWriter _lumpFileWriter;

		private readonly int _incrementalGCTimeSlice;

		private readonly bool _writeBarrierValidation;

		private readonly bool _developmentMode;

		protected readonly RuntimeProfile _profile;

		protected readonly DebuggerBuildOptions _debuggerBuildOptions;

		protected readonly RuntimeBuildType _runtimeLibrary;

		protected readonly Architecture _architecture;

		protected readonly IEnumerable<string> _specifiedLinkerFlags;

		protected readonly Features _il2cppConversionFeaturesFeatures;

		protected readonly DiagnosticOptions _il2CppConversionDiagnosticOptions;

		protected readonly TestingOptions _il2CppConversionTestingOptions;

		public bool ForceRebuildMapFileParser { get; private set; }

		public virtual NPath PchCSourceFile => null;

		public virtual NPath PchCppSourceFile => null;

		protected virtual bool PlatformSupportsLumping => true;

		public override IEnumerable<CppCompilationInstruction> CppCompileInstructions
		{
			get
			{
				foreach (CppCompilationInstruction item in from f in SourceFilesIn(_sourceDirectory)
					where f.Parent.FileName != "lumpedcpp"
					select f into sourceFile
					select CppCompilationInstructionFor(sourceFile, _cacheDirectory))
				{
					yield return item;
				}
				if (_runtimeLibrary != 0 && _runtimeLibrary != RuntimeBuildType.Tiny)
				{
					yield break;
				}
				foreach (CppCompilationInstruction item2 in LibIL2CPPCompileInstructions())
				{
					yield return item2;
				}
			}
		}

		public override IEnumerable<string> AdditionalCompilerFlags => _specifiedCompilerFlags ?? Enumerable.Empty<string>();

		protected NPath LibIL2CPPDir
		{
			get
			{
				if (CommonPaths.Il2CppRoot != null)
				{
					if (_runtimeLibrary == RuntimeBuildType.Tiny)
					{
						return CommonPaths.Il2CppRoot.Combine("libil2cpptiny");
					}
					return CommonPaths.Il2CppRoot.Combine("libil2cpp");
				}
				return null;
			}
		}

		private NPath BigLibIL2CPPDir
		{
			get
			{
				if (CommonPaths.Il2CppRoot != null)
				{
					return CommonPaths.Il2CppRoot.Combine("libil2cpp");
				}
				return null;
			}
		}

		protected virtual NPath BaselibUnityPlatformIncludeDir => CommonPaths.Il2CppRoot.Combine("external", "baselib", "Platforms", _platform.BaselibPlatformName, "Include");

		private NPath BaselibPlatformIncludeDir => BaselibUnityPlatformIncludeDir;

		private NPath BaselibIncludeDir => CommonPaths.Il2CppRoot.Combine("external", "baselib", "Include");

		protected static NPath BoehmDir
		{
			get
			{
				if (!(CommonPaths.Il2CppRoot != null))
				{
					return null;
				}
				return CommonPaths.Il2CppRoot.Combine("external/bdwgc");
			}
		}

		private static NPath xxHashDir
		{
			get
			{
				if (!(CommonPaths.Il2CppRoot != null))
				{
					return null;
				}
				return CommonPaths.Il2CppRoot.Combine("external/xxHash");
			}
		}

		private static bool HasMBedTLSDir
		{
			get
			{
				if (MonoSourceFileList.Available)
				{
					return MonoSourceFileList.MBedTLSDir.Exists();
				}
				return false;
			}
		}

		protected NPath PchDirectory => LibIL2CPPDir.Combine("pch");

		protected virtual IEnumerable<NPath> LibIL2CPPIncludeDirs
		{
			get
			{
				List<NPath> list = new List<NPath>();
				list.Add(LibIL2CPPDir);
				list.Add(BigLibIL2CPPDir);
				list.Add(BoehmDir.Combine("include"));
				list.Add(xxHashDir);
				list.Add(BaselibIncludeDir);
				list.Add(BaselibPlatformIncludeDir);
				list.Add(PchDirectory);
				List<NPath> list2 = list;
				if (HasMBedTLSDir)
				{
					list2.Add(MonoSourceFileList.MBedTLSDir.Combine("include"));
				}
				if (_il2CppConversionTestingOptions.HasFlag(TestingOptions.EnableGoogleBenchmark))
				{
					if (PlatformUtils.IsWindows())
					{
						list2.Add(CommonPaths.Il2CppDependencies.Combine("google-benchmark-win-x64/include"));
					}
					else if (PlatformUtils.IsOSX())
					{
						list2.Add(CommonPaths.Il2CppDependencies.Combine("google-benchmark-mac-x64/include"));
					}
				}
				return list2;
			}
		}

		private IEnumerable<NPath> LibIL2CPPDebuggerIncludeDirs => new NPath[1] { LibIL2CPPDir.Combine("debugger") };

		protected virtual IEnumerable<NPath> BoehmIncludeDirs => new NPath[2]
		{
			BoehmDir.Combine("include"),
			BoehmDir.Combine("libatomic_ops/src")
		};

		public override NPath GlobalCacheDirectory => _cacheDirectory;

		public NPath LibIL2CppDynamicLibraryLocation => (_cppToolChain.DynamicLibrariesHaveToSitNextToExecutable() ? OutputFile.Parent : _dataFolder).Combine("libil2cpp").ChangeExtension(_cppToolChain.DynamicLibraryExtension);

		public override IEnumerable<string> AdditionalLinkerFlags
		{
			get
			{
				if (_specifiedLinkerFlags == null)
				{
					yield break;
				}
				foreach (string specifiedLinkerFlag in _specifiedLinkerFlags)
				{
					yield return specifiedLinkerFlag;
				}
			}
		}

		public IL2CPPOutputBuildDescription(NPath sourceDirectory, NPath cacheDirectory, NPath outputFile, RuntimeProfile dotnetProfile, PlatformSupport platform, CppToolChain cppToolChain, NPath dataFolder, string relativeDataPath, bool forceRebuildMapFileParser, RuntimeBuildType runtimeLibrary, RuntimeGC runtimeGC, Architecture architecture, Features il2cppConversionFeatures = Features.None, DiagnosticOptions il2cppConversionDiagnosticOptions = DiagnosticOptions.None, TestingOptions il2cppConversionTestingOptions = TestingOptions.None, IEnumerable<string> additionalDefines = null, IEnumerable<NPath> additionalIncludeDirectories = null, IEnumerable<NPath> staticLibraries = null, IEnumerable<string> specifiedCompilerFlags = null, IEnumerable<string> specifiedLinkerFlags = null, NPath libil2cppCacheDirectory = null, NPath baselibDirectory = null, NPath mapFileParser = null, DebuggerBuildOptions debuggerBuildOptions = DebuggerBuildOptions.DebuggerDisabled, bool disableRuntimeLumping = false, bool writeBarrierValidation = false, int incrementalGCTimeSlice = 0, bool developmentMode = false)
		{
			_sourceDirectory = sourceDirectory;
			_lumpedSourceDirectory = sourceDirectory.Combine("lumpedcpp");
			_cacheDirectory = ((cacheDirectory != null) ? cacheDirectory.EnsureDirectoryExists() : null);
			if (debuggerBuildOptions == DebuggerBuildOptions.BuildDebuggerRuntimeCodeOnly || debuggerBuildOptions == DebuggerBuildOptions.DebuggerEnabled)
			{
				_monoSourceFileList = platform.GetDebuggerMonoSourceFileList();
			}
			else
			{
				_monoSourceFileList = platform.GetMonoSourceFileList();
			}
			_profile = dotnetProfile;
			_cppToolChain = cppToolChain;
			_platform = platform;
			ForceRebuildMapFileParser = forceRebuildMapFileParser;
			_staticLibraries = staticLibraries ?? new NPath[0];
			_additionalDefines = additionalDefines ?? new string[0];
			_additionalIncludeDirectories = additionalIncludeDirectories ?? new NPath[0];
			_outputFile = outputFile;
			_specifiedCompilerFlags = specifiedCompilerFlags;
			_specifiedLinkerFlags = specifiedLinkerFlags;
			_runtimeLibrary = runtimeLibrary;
			_runtimeGC = runtimeGC;
			_architecture = architecture;
			_dataFolder = dataFolder;
			_relativeDataPath = relativeDataPath;
			_mapFileParser = mapFileParser;
			_debuggerBuildOptions = debuggerBuildOptions;
			if (libil2cppCacheDirectory == null || libil2cppCacheDirectory == cacheDirectory)
			{
				_libil2cppCacheDirectory = CacheDirectoryFor(cacheDirectory, "libil2cpp");
			}
			else
			{
				_libil2cppCacheDirectory = libil2cppCacheDirectory;
			}
			_baselibDirectory = baselibDirectory;
			_additionalDefines = _additionalDefines.Concat(dotnetProfile.AdditionalCppDefines);
			_disableRuntimeLumping = disableRuntimeLumping;
			_lumpFileWriter = new LumpFileWriter(_lumpedSourceDirectory);
			_writeBarrierValidation = writeBarrierValidation;
			_incrementalGCTimeSlice = incrementalGCTimeSlice;
			_developmentMode = developmentMode;
			_il2cppConversionFeaturesFeatures = il2cppConversionFeatures;
			_il2CppConversionDiagnosticOptions = il2cppConversionDiagnosticOptions;
			_il2CppConversionTestingOptions = il2cppConversionTestingOptions;
		}

		public IL2CPPOutputBuildDescription(IL2CPPOutputBuildDescription other)
		{
			_sourceDirectory = other._sourceDirectory;
			_cacheDirectory = other._cacheDirectory;
			ForceRebuildMapFileParser = other.ForceRebuildMapFileParser;
			_staticLibraries = other._staticLibraries;
			_additionalDefines = other._additionalDefines;
			_additionalIncludeDirectories = other._additionalIncludeDirectories;
			_outputFile = other._outputFile;
			_specifiedCompilerFlags = other._specifiedCompilerFlags;
			_specifiedLinkerFlags = other._specifiedLinkerFlags;
			_monoSourceFileList = other._monoSourceFileList;
			_cppToolChain = other._cppToolChain;
			_platform = other._platform;
			_dataFolder = other._dataFolder;
			_relativeDataPath = other._relativeDataPath;
			_libil2cppCacheDirectory = other._libil2cppCacheDirectory;
			_baselibDirectory = other._baselibDirectory;
			_mapFileParser = other._mapFileParser;
			_runtimeLibrary = other._runtimeLibrary;
			_runtimeGC = other._runtimeGC;
			_architecture = other._architecture;
			_debuggerBuildOptions = other._debuggerBuildOptions;
			_profile = other._profile;
			_disableRuntimeLumping = other._disableRuntimeLumping;
			_lumpFileWriter = other._lumpFileWriter;
			_writeBarrierValidation = other._writeBarrierValidation;
			_incrementalGCTimeSlice = other._incrementalGCTimeSlice;
			_developmentMode = other._developmentMode;
			_il2cppConversionFeaturesFeatures = other._il2cppConversionFeaturesFeatures;
			_il2CppConversionDiagnosticOptions = other._il2CppConversionDiagnosticOptions;
			_il2CppConversionTestingOptions = other._il2CppConversionTestingOptions;
		}

		public static List<NPath> GetFoldersToLump(NPath runtimeLibraryPath, string[] exclusions)
		{
			return (from dir in runtimeLibraryPath.Directories()
				select dir.FileName into dir
				where !exclusions.Any((string exclusion) => dir.Contains(exclusion))
				select dir into folder
				select runtimeLibraryPath.Combine(folder)).ToList();
		}

		protected virtual IEnumerable<NPath> LibIL2CppSources()
		{
			return from f in SourceFilesIn(LibIL2CPPDir)
				where !f.ToString().Contains("cmake-build") && !f.ToString().Contains("out\\build")
				select f;
		}

		protected virtual IEnumerable<CppCompilationInstruction> LibIL2CPPCompileInstructions()
		{
			List<CppCompilationInstruction> list = new List<CppCompilationInstruction>();
			HashSet<NPath> hashSet = new HashSet<NPath>(LibIL2CppSources());
			if (_runtimeLibrary == RuntimeBuildType.Tiny)
			{
				foreach (NPath item2 in SourceFilesIn(BigLibIL2CPPDir.Combine("os"), BigLibIL2CPPDir.Combine("utils"), BigLibIL2CPPDir.Combine("vm-utils"), BigLibIL2CPPDir.Combine("gc"), BigLibIL2CPPDir.Combine("codegen")))
				{
					hashSet.Add(item2);
				}
			}
			if (!_disableRuntimeLumping && PlatformSupportsLumping)
			{
				foreach (NPath folder in GetFoldersToLump(LibIL2CPPDir, new string[3] { "external", "externals", "pch" }))
				{
					NPath sourceFile2 = _lumpFileWriter.Write(folder, hashSet);
					list.Add(CppCompilationInstructionFor(sourceFile2, _libil2cppCacheDirectory, new NPath[1] { folder }));
					NPath[] array = hashSet.Where((NPath f) => f.IsChildOf(folder) && f.HasExtension(".cpp")).ToArray();
					foreach (NPath item in array)
					{
						hashSet.Remove(item);
					}
				}
			}
			IEnumerable<NPath> enumerable = ((_runtimeLibrary != RuntimeBuildType.Tiny) ? XamarinAndroidSourceFiles().Concat(ZlibSourceFiles()) : Enumerable.Empty<NPath>());
			if (_profile == Profile.UnityTiny)
			{
				enumerable = enumerable.Concat(xxHashSourceFiles());
			}
			return list.Concat(BdwgcCompilationInstructions()).Concat(from sourceFile in hashSet.Concat(LibIl2CPPDebuggerSourceFiles().Concat(MBedTLSSourceFiles()).Concat(enumerable))
				select CppCompilationInstructionFor(sourceFile, _libil2cppCacheDirectory));
		}

		private IEnumerable<CppCompilationInstruction> BdwgcCompilationInstructions()
		{
			yield return CppCompilationInstructionFor(BoehmDir.Combine("extra/gc.c"), _libil2cppCacheDirectory, new NPath[1] { BoehmDir });
			yield return CppCompilationInstructionFor(BoehmDir.Combine("extra/krait_signal_handler.c"), _libil2cppCacheDirectory);
		}

		private IEnumerable<NPath> MBedTLSSourceFiles()
		{
			return Enumerable.Empty<NPath>();
		}

		private IEnumerable<NPath> XamarinAndroidSourceFiles()
		{
			if (!(CommonPaths.Il2CppRoot != null))
			{
				return Enumerable.Empty<NPath>();
			}
			return SourceFilesIn(CommonPaths.Il2CppRoot.Combine("external/xamarin-android"));
		}

		private IEnumerable<NPath> ZlibSourceFiles()
		{
			if (!(CommonPaths.Il2CppRoot != null))
			{
				return Enumerable.Empty<NPath>();
			}
			return SourceFilesIn(CommonPaths.Il2CppRoot.Combine("external/zlib"));
		}

		private IEnumerable<NPath> xxHashSourceFiles()
		{
			if (!(CommonPaths.Il2CppRoot != null))
			{
				return Enumerable.Empty<NPath>();
			}
			return new NPath[1] { CommonPaths.Il2CppRoot.Combine("external/xxHash/xxhash.c") };
		}

		private IEnumerable<NPath> SpecificLibIL2CPPFiles()
		{
			yield return LibIL2CPPDir.Combine("char-conversions.cpp");
		}

		protected IEnumerable<NPath> LibIl2CPPOsApiSourceFiles()
		{
			return SourceFilesIn(LibIL2CPPDir.Combine("os"), LibIL2CPPDir.Combine("utils"));
		}

		protected IEnumerable<NPath> LibIl2CPPDebuggerSourceFiles()
		{
			if (!ShouldBuildDebuggerRuntimeCode())
			{
				return Enumerable.Empty<NPath>();
			}
			List<NPath> list = new List<NPath>();
			list.AddRange(_monoSourceFileList.GetEGLibSourceFiles(_architecture));
			list.AddRange(_monoSourceFileList.GetMetadataDebuggerSourceFiles(_architecture));
			list.AddRange(_monoSourceFileList.GetMiniSourceFiles(_architecture));
			list.AddRange(_monoSourceFileList.GetUtilsSourceFiles(_architecture));
			return list;
		}

		protected CppCompilationInstruction CppCompilationInstructionFor(NPath sourceFile, NPath cacheDirectory, IEnumerable<NPath> lumpDirectories = null)
		{
			return new CppCompilationInstruction
			{
				SourceFile = sourceFile,
				Defines = AdditionalDefinesFor(sourceFile),
				IncludePaths = AdditionalIncludePathsFor(sourceFile),
				LumpPaths = ((lumpDirectories != null) ? lumpDirectories : Enumerable.Empty<NPath>()),
				CompilerFlags = AdditionalCompilerFlags,
				CacheDirectory = cacheDirectory,
				TreatWarningsAsErrors = (!_monoSourceFileList.IsMonoFile(sourceFile) && !IsZlibFile(sourceFile) && !IsxxHashFile(sourceFile))
			};
		}

		public virtual IEnumerable<NPath> SourceFilesIn(params NPath[] foldersToGlob)
		{
			return from f in foldersToGlob.SelectMany((NPath d) => d.Files("*.c*", recurse: true))
				where f.HasExtension("c", "cpp") && !f.Parent.Equals(PchDirectory)
				select f;
		}

		protected virtual bool IsBoehmFile(NPath sourceFile)
		{
			return sourceFile.IsChildOf(BoehmDir);
		}

		private bool IsZlibFile(NPath sourceFile)
		{
			return sourceFile.Parent.FileName == "zlib";
		}

		private bool IsxxHashFile(NPath sourceFile)
		{
			return sourceFile.Parent.FileName == "xxHash";
		}

		protected virtual bool IsLibIL2CPPFile(NPath sourceFile)
		{
			if (sourceFile.FileName.Equals("pch-cpp.cpp") || sourceFile.FileName.Equals("pch-c.c") || !sourceFile.IsChildOf(LibIL2CPPDir))
			{
				return _lumpFileWriter.IsLumpFile(sourceFile);
			}
			return true;
		}

		public override IEnumerable<string> AdditionalDefinesFor(NPath sourceFile)
		{
			foreach (string additionalDefine in _additionalDefines)
			{
				yield return additionalDefine;
			}
			if (ShouldBuildDebuggerRuntimeCode())
			{
				if (_debuggerBuildOptions == DebuggerBuildOptions.DebuggerEnabled)
				{
					yield return "IL2CPP_MONO_DEBUGGER=1";
				}
				yield return $"IL2CPP_DEBUGGER_PORT={_platform.GetDebuggerFixedPort()}";
				if (IsLibIL2CPPFile(sourceFile))
				{
					foreach (string item in MonoDefines())
					{
						yield return item;
					}
				}
			}
			else
			{
				yield return "IL2CPP_MONO_DEBUGGER_DISABLED";
			}
			if (IsBoehmFile(sourceFile))
			{
				foreach (string item2 in GCDefines(_runtimeGC))
				{
					yield return item2;
				}
			}
			if (_runtimeLibrary == RuntimeBuildType.LibIL2CPPDynamic)
			{
				yield return IsLibIL2CPPFile(sourceFile) ? "LIBIL2CPP_EXPORT_CODEGEN_API" : "LIBIL2CPP_IMPORT_CODEGEN_API";
			}
			if (_runtimeGC == RuntimeGC.BDWGC)
			{
				yield return "GC_NOT_DLL";
			}
			if (_runtimeLibrary == RuntimeBuildType.LibIL2CPPStatic || _runtimeLibrary == RuntimeBuildType.LibIL2CPPDynamic || _runtimeLibrary == RuntimeBuildType.Tiny)
			{
				yield return "RUNTIME_IL2CPP";
				if (_monoSourceFileList.IsMonoFile(sourceFile))
				{
					foreach (string item3 in MonoDefines())
					{
						yield return item3;
					}
				}
			}
			if (_writeBarrierValidation)
			{
				yield return "IL2CPP_ENABLE_STRICT_WRITE_BARRIERS=1";
				yield return "IL2CPP_ENABLE_WRITE_BARRIER_VALIDATION=1";
			}
			if (_incrementalGCTimeSlice != 0 || _writeBarrierValidation)
			{
				yield return "IL2CPP_ENABLE_WRITE_BARRIERS=1";
				yield return $"IL2CPP_INCREMENTAL_TIME_SLICE={_incrementalGCTimeSlice}";
			}
			if (_il2CppConversionDiagnosticOptions.HasFlag(DiagnosticOptions.EnableTinyDebugging))
			{
				yield return "IL2CPP_TINY_DEBUG";
			}
			if (_cppToolChain.BuildConfiguration == BuildConfiguration.Debug)
			{
				yield return "IL2CPP_DEBUG=1";
			}
			if (_runtimeLibrary == RuntimeBuildType.Tiny && _developmentMode)
			{
				yield return "IL2CPP_TINY_DEBUG_METADATA";
			}
			if (_il2CppConversionTestingOptions.HasFlag(TestingOptions.EnableGoogleBenchmark))
			{
				yield return "IL2CPP_GOOGLE_BENCHMARK";
			}
			if (_il2cppConversionFeaturesFeatures.HasFlag(Features.EnableReload))
			{
				yield return "IL2CPP_ENABLE_RELOAD";
			}
			yield return "BASELIB_INLINE_NAMESPACE=il2cpp_baselib";
			if (!string.IsNullOrEmpty(_relativeDataPath))
			{
				yield return "IL2CPP_DEFAULT_DATA_DIR_PATH=" + _relativeDataPath.Replace('\\', '/');
			}
		}

		public override IEnumerable<NPath> AdditionalIncludePathsFor(NPath sourceFile)
		{
			if (IsBoehmFile(sourceFile))
			{
				return _additionalIncludeDirectories.Concat(BoehmIncludeDirs);
			}
			if (IsLibIL2CPPFile(sourceFile))
			{
				if (ShouldBuildDebuggerRuntimeCode() && IsLibIL2CPPFile(sourceFile))
				{
					return _additionalIncludeDirectories.Concat(LibIL2CPPIncludeDirs).Concat(_monoSourceFileList.MonoIncludeDirs);
				}
				return _additionalIncludeDirectories.Concat(LibIL2CPPIncludeDirs);
			}
			IEnumerable<NPath> enumerable = _additionalIncludeDirectories.Concat(LibIL2CPPIncludeDirs.Append(_sourceDirectory));
			if (_monoSourceFileList.IsMonoEglibFile(sourceFile))
			{
				return enumerable.Concat(_monoSourceFileList.MonoEglibIncludeDirs);
			}
			if (_monoSourceFileList.IsMonoFile(sourceFile))
			{
				IEnumerable<NPath> enumerable2 = enumerable.Concat(_monoSourceFileList.MonoIncludeDirs);
				if (_monoSourceFileList.IsMonoDebuggerFile(sourceFile))
				{
					enumerable2 = enumerable2.Concat(LibIL2CPPDebuggerIncludeDirs);
				}
				return enumerable2;
			}
			return enumerable;
		}

		public override IEnumerable<NPath> GetStaticLibraries(BuildConfiguration configuration)
		{
			foreach (NPath staticLibrary in _staticLibraries)
			{
				yield return staticLibrary;
			}
			if (_platform.BaselibBuildType == BaselibBuildType.StaticLibrary)
			{
				yield return BaselibLibrary("baselib" + _cppToolChain.StaticLibraryExtension, configuration);
			}
			if (!_il2CppConversionTestingOptions.HasFlag(TestingOptions.EnableGoogleBenchmark))
			{
				yield break;
			}
			if (PlatformUtils.IsWindows())
			{
				if (_cppToolChain.BuildConfiguration == BuildConfiguration.Debug)
				{
					yield return CommonPaths.Il2CppDependencies.Combine("google-benchmark-win-x64/lib/Debug/benchmark.lib");
				}
				else
				{
					yield return CommonPaths.Il2CppDependencies.Combine("google-benchmark-win-x64/lib/Release/benchmark.lib");
				}
				yield return new NPath("shlwapi.lib");
			}
			else if (PlatformUtils.IsOSX())
			{
				yield return CommonPaths.Il2CppDependencies.Combine("google-benchmark-mac-x64/lib/libbenchmark.a");
			}
		}

		public override IEnumerable<NPath> GetDynamicLibraries(BuildConfiguration configuration)
		{
			if (_runtimeLibrary == RuntimeBuildType.LibIL2CPPDynamic)
			{
				yield return LibIL2CppDynamicLibraryLocation;
			}
			if (_platform.BaselibBuildType == BaselibBuildType.DynamicLibrary)
			{
				yield return BaselibLibrary("baselib" + _cppToolChain.DynamicLibraryExtension, configuration);
			}
		}

		protected virtual IEnumerable<string> MonoDefines()
		{
			if (ShouldBuildDebuggerRuntimeCode())
			{
				yield return "PLATFORM_UNITY";
				yield return "UNITY_USE_PLATFORM_STUBS";
			}
			yield return "ENABLE_OVERRIDABLE_ALLOCATORS";
			yield return "IL2CPP_ON_MONO=1";
			yield return "DISABLE_JIT=1";
			yield return "DISABLE_REMOTING=1";
			yield return "HAVE_CONFIG_H";
			yield return "MONO_DLL_EXPORT=1";
		}

		private bool ShouldBuildDebuggerRuntimeCode()
		{
			if (_debuggerBuildOptions != DebuggerBuildOptions.BuildDebuggerRuntimeCodeOnly)
			{
				return _debuggerBuildOptions == DebuggerBuildOptions.DebuggerEnabled;
			}
			return true;
		}

		protected virtual IEnumerable<string> GCDefines(RuntimeGC runtimeGC)
		{
			switch (runtimeGC)
			{
			case RuntimeGC.BDWGC:
				yield return "HAVE_BDWGC_GC";
				yield return "HAVE_BOEHM_GC";
				yield return "DEFAULT_GC_NAME=\"BDWGC\"";
				foreach (string item in BoehmDefines())
				{
					yield return item;
				}
				break;
			case RuntimeGC.SGEN:
				yield return "HAVE_SGEN_GC";
				yield return "DEFAULT_GC_NAME=\"SGEN\"";
				break;
			default:
				throw new NotSupportedException($"GCDefines: unknown runtime garbage collector {runtimeGC}");
			}
		}

		protected virtual IEnumerable<string> BoehmDefines()
		{
			yield return "ALL_INTERIOR_POINTERS=1";
			yield return "GC_GCJ_SUPPORT=1";
			yield return "JAVA_FINALIZATION=1";
			yield return "NO_EXECUTE_PERMISSION=1";
			yield return "GC_NO_THREADS_DISCOVERY=1";
			yield return "IGNORE_DYNAMIC_LOADING=1";
			yield return "GC_DONT_REGISTER_MAIN_STATIC_DATA=1";
			yield return "GC_VERSION_MAJOR=7";
			yield return "GC_VERSION_MINOR=7";
			yield return "GC_VERSION_MICRO=0";
			yield return "GC_THREADS=1";
			yield return "USE_MMAP=1";
			yield return "USE_MUNMAP=1";
			if (_runtimeLibrary == RuntimeBuildType.Tiny)
			{
				yield return "DONT_USE_ATEXIT=1";
				yield return "NO_GETENV=1";
			}
		}

		public override void FinalizeBuild(CppToolChain toolChain)
		{
			if (toolChain.SupportsMapFileParser)
			{
				if (_mapFileParser == null || !_mapFileParser.Exists())
				{
					_mapFileParser = BuildMapFileParser();
				}
				RunMapFileParser(toolChain, OutputFile, _mapFileParser);
			}
			if (_sourceDirectory != _outputFile.Parent && _sourceDirectory.DirectoryExists("Data"))
			{
				_sourceDirectory.Combine("Data").Copy(_outputFile.Parent.MakeAbsolute());
			}
			base.FinalizeBuild(toolChain);
		}

		private void RunMapFileParser(CppToolChain toolChain, NPath outputFile, NPath mapFileParser)
		{
			NPath arg = outputFile.ChangeExtension("map");
			NPath arg2 = _dataFolder.Combine("SymbolMap");
			string text = $"-format={toolChain.MapFileParserFormat} \"{arg}\" \"{arg2}\"";
			ConsoleOutput.Info.WriteLine("Encoding map file using command: {0} {1}", mapFileParser, text);
			using (MiniProfiler.Section("Running MapFileParser"))
			{
				Shell.ExecuteResult executeResult = BuildShell.Execute(new Shell.ExecuteArgs
				{
					Executable = mapFileParser,
					Arguments = text
				});
				string arg3 = executeResult.StdErr.Trim() + executeResult.StdOut.Trim();
				if (executeResult.ExitCode != 0)
				{
					throw new Exception(string.Format("Process {0} ended with exitcode {1}" + Environment.NewLine + "{2}" + Environment.NewLine, mapFileParser, executeResult.ExitCode, arg3));
				}
			}
		}

		private NPath BuildMapFileParser()
		{
			using (MiniProfiler.Section("BuildMapFileParser"))
			{
				NPath cacheDirectory = ((_cacheDirectory != null) ? _cacheDirectory.Combine("MapFileParserCache").EnsureDirectoryExists() : null);
				return CppProgramBuilder.Create(RuntimePlatform.Current, new MapFileParserBuildDescription(cacheDirectory), verbose: false, Architecture.BestThisMachineCanRun, BuildConfiguration.Release, ForceRebuildMapFileParser, treatWarningsAsErrors: false, includeFileNamesInHashes: false, assemblyOutput: false, null).BuildAndLogStatsForTestRunner();
			}
		}

		public override void OnBeforeLink(HeaderFileHashProvider headerHashProvider, NPath workingDirectory, IEnumerable<NPath> objectFiles, CppToolChainContext toolChainContext, bool forceRebuild, bool verbose, bool includeFileNamesInHashes)
		{
			if (_runtimeLibrary == RuntimeBuildType.LibIL2CPPDynamic)
			{
				LibIL2CPPDynamicLibProgramDescription programBuildDescription = new LibIL2CPPDynamicLibProgramDescription(LibIL2CPPCompileInstructions(), LibIL2CppDynamicLibraryLocation, _libil2cppCacheDirectory);
				new CppProgramBuilder(_cppToolChain, programBuildDescription, headerHashProvider, verbose, forceRebuild, includeFileNamesInHashes).BuildAndLogStatsForTestRunner();
			}
		}

		protected static NPath CacheDirectoryFor(NPath cacheDirectory, string nameSuggestion)
		{
			if (!(cacheDirectory != null))
			{
				return TempDir.Empty(nameSuggestion);
			}
			return cacheDirectory.Combine(nameSuggestion).EnsureDirectoryExists();
		}

		public override bool AllowCompilation()
		{
			return !_il2CppConversionTestingOptions.HasFlag(TestingOptions.BlockCompiling);
		}

		private NPath BaselibLibrary(string baselibLibraryName, BuildConfiguration configuration)
		{
			NPath nPath = ((_baselibDirectory != null) ? _baselibDirectory.Combine(baselibLibraryName) : CommonPaths.Il2CppBuild.Root.Combine("baselib/" + ((configuration == BuildConfiguration.Debug) ? "debug" : "release") + "_" + _platform.BaselibToolchainName(_architecture) + "/" + baselibLibraryName));
			if (!nPath.FileExists())
			{
				string arg = string.Empty;
				if (!UnitySourceCode.Available && Il2CppDependencies.Available)
				{
					arg = "\nA test fixture may be missing a OneTimeSetUp to build baselib.  You can fix this or run perl test.pl <platform> --build-baselib-only (or just perl test.pl --build-baselib-only to build all platforms) in the root of the IL2CPP repo to build it.";
				}
				throw new BuilderFailedException($"The Baselib library '{nPath}' does not exist and is required for this build.{arg}");
			}
			return nPath;
		}
	}
}
