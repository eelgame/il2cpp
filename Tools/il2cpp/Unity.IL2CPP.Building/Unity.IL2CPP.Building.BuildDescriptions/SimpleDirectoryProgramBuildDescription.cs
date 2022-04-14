using System.Collections.Generic;
using System.Linq;
using NiceIO;

namespace Unity.IL2CPP.Building.BuildDescriptions
{
	public class SimpleDirectoryProgramBuildDescription : ProgramBuildDescription, IHaveSourceDirectories
	{
		private readonly NPath _sourceDirectory;

		private readonly IEnumerable<string> _additionalCompilerFlags;

		private readonly IEnumerable<string> _additionalLinkerFlags;

		private readonly IEnumerable<NPath> _additionalIncludeDirectories;

		private readonly IEnumerable<string> _additionalDefines;

		private readonly NPath _cacheDirectory;

		public IEnumerable<NPath> SourceDirectories
		{
			get
			{
				yield return _sourceDirectory;
			}
		}

		public override NPath GlobalCacheDirectory => _cacheDirectory;

		public override IEnumerable<CppCompilationInstruction> CppCompileInstructions => from f in _sourceDirectory.Files("*.cpp")
			select new CppCompilationInstruction
			{
				SourceFile = f,
				CompilerFlags = _additionalCompilerFlags,
				IncludePaths = new NPath[1] { _sourceDirectory }.Concat(_additionalIncludeDirectories),
				Defines = _additionalDefines,
				CacheDirectory = _cacheDirectory
			};

		public override IEnumerable<string> AdditionalCompilerFlags => _additionalCompilerFlags;

		public override IEnumerable<string> AdditionalLinkerFlags => _additionalLinkerFlags;

		public SimpleDirectoryProgramBuildDescription(NPath sourceDir, NPath outputFile, NPath cacheDirectory, IEnumerable<string> additionalCompilerFlags = null, IEnumerable<string> additionalLinkerFlags = null, IEnumerable<NPath> additionalIncludeDirectories = null, IEnumerable<string> additionalDefines = null)
		{
			_sourceDirectory = sourceDir;
			_outputFile = outputFile;
			_additionalCompilerFlags = additionalCompilerFlags ?? Enumerable.Empty<string>();
			_additionalLinkerFlags = additionalLinkerFlags ?? Enumerable.Empty<string>();
			_additionalIncludeDirectories = additionalIncludeDirectories ?? Enumerable.Empty<NPath>();
			_additionalDefines = additionalDefines ?? Enumerable.Empty<string>();
			_cacheDirectory = cacheDirectory;
		}
	}
}
