using System;
using System.Collections.Generic;
using System.IO;
using NiceIO;
using Unity.IL2CPP.Building.BuildDescriptions;
using Unity.IL2CPP.Common;

namespace Unity.IL2CPP.Building
{
	public class BuildingOptions
	{
		public Architecture Architecture = Architecture.OfCurrentProcess;

		public BuildConfiguration Configuration;

		public NPath ToolChainPath;

		public bool UseDependenciesToolChain;

		public bool DisableExceptions;

		public bool EnableScriptDebugging;

		public NPath DataFolder;

		public string RelativeDataPath;

		public NPath SysrootPath;

		public NPath SourceDirectory;

		public NPath OutputPath;

		public IEnumerable<string> AdditionalDefines = new string[0];

		public IEnumerable<string> AdditionalLibraries = new string[0];

		public IEnumerable<NPath> AdditionalIncludeDirectories = new NPath[0];

		public IEnumerable<NPath> AdditionalLinkDirectories = new NPath[0];

		public IEnumerable<NPath> AdditionalCpp = new NPath[0];

		public string CompilerFlags;

		public string LinkerFlags;

		public RuntimeBuildType Runtime;

		public RuntimeGC RuntimeGC;

		public NPath LibIL2CPPCacheDirectory;

		public NPath BaselibDirectory;

		public bool Verbose;

		public NPath CacheDirectory;

		public bool ForceRebuild;

		public bool TreatWarningsAsErrors;

		public bool IncludeFileNamesInHashes;

		public bool AssemblyOutput;

		public bool DontLinkCrt;

		public string AssemblyMethod;

		public bool DisableRuntimeLumping;

		public bool SetEnvironmentVariables;

		public string ShowIncludes;

		public bool AvoidDynamicLibraryCopy;

		public string GenerateCmake;

		public BuildShell.CommandLogMode CommandLog;

		public void Validate()
		{
			foreach (string additionalLibrary in AdditionalLibraries)
			{
				try
				{
					if ((File.GetAttributes(additionalLibrary) & FileAttributes.Directory) != 0)
					{
						throw new ArgumentException($"Cannot specify directory \"{additionalLibrary}\" as an additional library file.", "--additional-libraries");
					}
				}
				catch (FileNotFoundException innerException)
				{
					throw new ArgumentException($"Non-existent file \"{additionalLibrary}\" specified as an additional library file.", "--additional-libraries", innerException);
				}
				catch (DirectoryNotFoundException innerException2)
				{
					throw new ArgumentException($"Non-existent directory \"{additionalLibrary}\" specified as an additional library file.  Cannot specify a directory as an additional library.", "--additional-libraries", innerException2);
				}
				catch (ArgumentException)
				{
					throw;
				}
				catch (Exception innerException3)
				{
					throw new ArgumentException($"Unknown error with additional library parameter \"{additionalLibrary}\".", "--additional-libraries", innerException3);
				}
			}
		}
	}
}
