using System.Collections.Generic;
using System.Linq;
using NiceIO;
using Unity.IL2CPP.Building.Platforms;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Common.Profiles;
using Unity.IL2CPP.Options;

namespace Unity.IL2CPP.Building.BuildDescriptions
{
	public class UnitTestPlusPlusBuildDescription : IL2CPPOutputBuildDescription
	{
		private NPath m_unitTestPlusPlusCacheDirectory;

		private bool m_usePosixFiles;

		protected override IEnumerable<NPath> LibIL2CPPIncludeDirs => base.LibIL2CPPIncludeDirs.Concat(new NPath[1] { UnitTestPlusPlusInstall.Current.Path });

		public UnitTestPlusPlusBuildDescription(NPath sourceDirectory, NPath cacheDirectory, NPath outputFile, RuntimeProfile dotnetProfile, PlatformSupport platform, CppToolChain cppToolChain, NPath dataFolder, string relativeDataPath, bool forceRebuildMapFileParser, RuntimeBuildType runtimeLibrary, RuntimeGC runtimeGC, Architecture architecture, Features il2cppConversionFeatures = Features.None, DiagnosticOptions il2CppConversionDiagnosticOptions = DiagnosticOptions.None, TestingOptions il2CppConversionTestingOptions = TestingOptions.None, IEnumerable<string> additionalDefines = null, IEnumerable<NPath> additionalIncludeDirectories = null, IEnumerable<NPath> staticLibraries = null, IEnumerable<string> specifiedCompilerFlags = null, IEnumerable<string> specifiedLinkerFlags = null, NPath libil2cppCacheDirectory = null, NPath mapFileParser = null, bool usePosixFiles = false)
			: base(sourceDirectory, cacheDirectory, outputFile, dotnetProfile, platform, cppToolChain, dataFolder, relativeDataPath, forceRebuildMapFileParser, runtimeLibrary, runtimeGC, architecture, il2cppConversionFeatures, il2CppConversionDiagnosticOptions, il2CppConversionTestingOptions, additionalDefines, additionalIncludeDirectories, staticLibraries, specifiedCompilerFlags, specifiedLinkerFlags, libil2cppCacheDirectory, mapFileParser)
		{
			m_unitTestPlusPlusCacheDirectory = IL2CPPOutputBuildDescription.CacheDirectoryFor(cacheDirectory, "UnitTestPlusPlus");
			m_usePosixFiles = usePosixFiles;
		}

		public UnitTestPlusPlusBuildDescription(UnitTestPlusPlusBuildDescription other)
			: base(other)
		{
			m_unitTestPlusPlusCacheDirectory = other.m_unitTestPlusPlusCacheDirectory;
			m_usePosixFiles = other.m_usePosixFiles;
		}

		public override IEnumerable<string> AdditionalDefinesFor(NPath sourceFile)
		{
			return base.AdditionalDefinesFor(sourceFile).Concat(new string[3] { "ENABLE_UNIT_TESTS", "RUNTIME_NONE", "NET_4_0" });
		}

		public IEnumerable<CppCompilationInstruction> UnitTestPlusPlusLibIL2CPPCompileInstructions()
		{
			return LibIL2CPPOsApiCompilationInstructions().Concat(UnitTestPlusPlusCompilationInstructions());
		}

		protected override IEnumerable<CppCompilationInstruction> LibIL2CPPCompileInstructions()
		{
			return LibIL2CPPOsApiCompilationInstructions().Concat(UnitTestPlusPlusCompilationInstructions());
		}

		private IEnumerable<CppCompilationInstruction> UnitTestPlusPlusCompilationInstructions()
		{
			if (m_usePosixFiles)
			{
				return from sourceFile in SourceFilesInHelper(UnitTestPlusPlusInstall.Current.Path).Append(UnitTestPlusPlusInstall.Current.Path.Combine("Posix/SignalTranslator.cpp"))
					select CppCompilationInstructionFor(sourceFile, m_unitTestPlusPlusCacheDirectory);
			}
			return from sourceFile in SourceFilesInHelper(UnitTestPlusPlusInstall.Current.Path)
				select CppCompilationInstructionFor(sourceFile, m_unitTestPlusPlusCacheDirectory);
		}

		private IEnumerable<CppCompilationInstruction> LibIL2CPPOsApiCompilationInstructions()
		{
			return from sourceFile in LibIl2CPPOsApiSourceFiles()
				select CppCompilationInstructionFor(sourceFile, m_unitTestPlusPlusCacheDirectory);
		}

		public IEnumerable<NPath> SourceFilesInHelper(params NPath[] foldersToGlob)
		{
			return from f in foldersToGlob.SelectMany((NPath d) => d.Files("*.c*"))
				where f.HasExtension("c", "cpp")
				select f;
		}
	}
}
