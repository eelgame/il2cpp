using System.Collections.Generic;
using System.Linq;
using NiceIO;
using Unity.IL2CPP.Building.Hashing;
using Unity.IL2CPP.Building.ToolChains;
using Unity.IL2CPP.Common;

namespace Unity.IL2CPP.Building.BuildDescriptions
{
	public abstract class ProgramBuildDescription
	{
		protected NPath _outputFile;

		public abstract IEnumerable<CppCompilationInstruction> CppCompileInstructions { get; }

		public abstract NPath GlobalCacheDirectory { get; }

		public virtual NPath OutputFile
		{
			get
			{
				if (_outputFile != null)
				{
					return _outputFile;
				}
				if (GlobalCacheDirectory != null)
				{
					_outputFile = GlobalCacheDirectory.Combine("program");
					return _outputFile;
				}
				_outputFile = TempDir.Root.Combine("program");
				return _outputFile;
			}
		}

		public virtual IEnumerable<string> AdditionalCompilerFlags => Enumerable.Empty<string>();

		public virtual IEnumerable<string> AdditionalLinkerFlags => Enumerable.Empty<string>();

		public virtual IEnumerable<string> AdditionalDefinesFor(NPath path)
		{
			return Enumerable.Empty<string>();
		}

		public virtual IEnumerable<NPath> AdditionalIncludePathsFor(NPath path)
		{
			return Enumerable.Empty<NPath>();
		}

		public virtual IEnumerable<NPath> GetStaticLibraries(BuildConfiguration configuration)
		{
			return Enumerable.Empty<NPath>();
		}

		public virtual IEnumerable<NPath> GetDynamicLibraries(BuildConfiguration configuration)
		{
			yield break;
		}

		public virtual void FinalizeBuild(CppToolChain toolChain)
		{
			toolChain.FinalizeBuild(this);
		}

		public virtual void OnBeforeLink(HeaderFileHashProvider headerHashProvider, NPath workingDirectory, IEnumerable<NPath> objectFiles, CppToolChainContext toolChainContext, bool forceRebuild, bool verbose, bool includeFileNamesInHashes)
		{
		}

		public virtual IEnumerable<string> CompilerFlagsFor(CppCompilationInstruction cppCompilationInstruction)
		{
			return Enumerable.Empty<string>();
		}

		public virtual bool AllowCompilation()
		{
			return true;
		}
	}
}
