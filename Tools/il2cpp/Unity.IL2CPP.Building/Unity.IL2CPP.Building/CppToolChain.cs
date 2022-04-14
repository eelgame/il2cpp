using System;
using System.Collections.Generic;
using System.Linq;
using NiceIO;
using Unity.IL2CPP.Building.BuildDescriptions;
using Unity.IL2CPP.Building.Hashing;
using Unity.IL2CPP.Building.ToolChains;
using Unity.IL2CPP.Common;

namespace Unity.IL2CPP.Building
{
	public abstract class CppToolChain
	{
		public Architecture Architecture { get; private set; }

		public BuildConfiguration BuildConfiguration { get; private set; }

		public virtual bool SupportsMapFileParser => false;

		public virtual string MapFileParserFormat
		{
			get
			{
				throw new NotImplementedException("The base class version of this  property should never be called. Does the derived class need to override it?");
			}
		}

		public abstract string DynamicLibraryExtension { get; }

		public abstract string StaticLibraryExtension { get; }

		protected CppToolChain(Architecture architecture, BuildConfiguration buildConfiguration)
		{
			Architecture = architecture;
			BuildConfiguration = buildConfiguration;
		}

		public abstract IEnumerable<string> ToolChainDefines();

		public abstract IEnumerable<NPath> ToolChainIncludePaths();

		public abstract IEnumerable<string> OutputArgumentFor(NPath objectFile, NPath sourceFile);

		public abstract string ObjectExtension();

		public abstract string ExecutableExtension();

		public abstract bool CanBuildInCurrentEnvironment();

		public virtual string GetCannotBuildInCurrentEnvironmentErrorMessage()
		{
			return null;
		}

		public virtual CompilationResult ShellResultToCompilationResult(Shell.ExecuteResult shellResult)
		{
			return new CompilationResult
			{
				Duration = shellResult.Duration,
				Success = (shellResult.ExitCode == 0),
				InterestingOutput = GetInterestingOutputFromCompilationShellResult(shellResult)
			};
		}

		protected virtual string GetInterestingOutputFromCompilationShellResult(Shell.ExecuteResult shellResult)
		{
			return shellResult.StdOut;
		}

		public virtual LinkerResult ShellResultToLinkerResult(Shell.ExecuteResult shellResult)
		{
			return new LinkerResult
			{
				Duration = shellResult.Duration,
				Success = (shellResult.ExitCode == 0),
				InterestingOutput = GetInterestingOutputFromLinkerShellResult(shellResult)
			};
		}

		public virtual IEnumerable<NPath> ToolChainLibraryPaths()
		{
			yield break;
		}

		protected virtual string GetInterestingOutputFromLinkerShellResult(Shell.ExecuteResult shellResult)
		{
			return shellResult.StdOut;
		}

		public virtual Dictionary<string, string> EnvVars()
		{
			return null;
		}

		public virtual IEnumerable<string> ToolChainStaticLibraries()
		{
			yield break;
		}

		public virtual IEnumerable<Type> SupportedArchitectures()
		{
			return new Type[2]
			{
				typeof(x86Architecture),
				typeof(x64Architecture)
			};
		}

		public virtual IEnumerable<NPath> PrecompiledHeaderObjectFiles()
		{
			return null;
		}

		public abstract IEnumerable<string> CompilerFlagsFor(CppCompilationInstruction cppCompilationInstruction);

		public virtual CppToolChainContext CreateToolChainContext()
		{
			return new CppToolChainContext();
		}

		public abstract NPath CompilerExecutableFor(NPath sourceFile);

		public abstract CppProgramBuilder.LinkerInvocation MakeLinkerInvocation(IEnumerable<NPath> objectFiles, NPath outputFile, IEnumerable<NPath> staticLibraries, IEnumerable<NPath> dynamicLibraries, IEnumerable<string> specifiedLinkerFlags, CppToolChainContext toolChainContext);

		public virtual void OnBeforeCompile(ProgramBuildDescription programBuildDescription, CppToolChainContext toolChainContext, HeaderFileHashProvider headerHashProvider, NPath workingDirectory, bool forceRebuild, bool verbose, bool includeFileNamesInHashes)
		{
		}

		public virtual void OnBeforeLink(ProgramBuildDescription programBuildDescription, NPath workingDirectory, IEnumerable<NPath> objectFiles, CppToolChainContext toolChainContext, bool forceRebuild, bool verbose)
		{
		}

		public virtual void OnAfterLink(NPath outputFile, CppToolChainContext toolChainContext, bool forceRebuild, bool verbose)
		{
		}

		public virtual void FinalizeBuild(ProgramBuildDescription programBuildDescription)
		{
		}

		protected IEnumerable<string> ChooseCompilerFlags(CppCompilationInstruction cppCompilationInstruction, Func<CppCompilationInstruction, IEnumerable<string>> defaultCompilerFlags)
		{
			return defaultCompilerFlags(cppCompilationInstruction).Concat(cppCompilationInstruction.CompilerFlags);
		}

		protected IEnumerable<string> ChooseLinkerFlags(IEnumerable<NPath> staticLibraries, IEnumerable<NPath> dynamicLibraries, NPath outputFile, IEnumerable<string> specifiedLinkerFlags, Func<IEnumerable<NPath>, IEnumerable<NPath>, NPath, IEnumerable<string>> defaultLinkerFlags)
		{
			return defaultLinkerFlags(staticLibraries, dynamicLibraries, outputFile).Concat(specifiedLinkerFlags);
		}

		public virtual bool DynamicLibrariesHaveToSitNextToExecutable()
		{
			return false;
		}

		public virtual NPath GetLibraryFileName(NPath library)
		{
			return library;
		}

		public virtual string GetToolchainInfoForOutput()
		{
			return GetType().Name;
		}

		public virtual bool CanGenerateAssemblyCode()
		{
			return false;
		}

		public virtual SourceCodeSearcher SourceCodeSearcher()
		{
			return null;
		}
	}
}
