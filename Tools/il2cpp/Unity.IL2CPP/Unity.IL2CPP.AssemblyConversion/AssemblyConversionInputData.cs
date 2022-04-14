using System.Collections.ObjectModel;
using Mono.Cecil;
using NiceIO;
using Unity.IL2CPP.Common.Profiles;

namespace Unity.IL2CPP.AssemblyConversion
{
	public class AssemblyConversionInputData
	{
		public readonly NPath OutputDir;

		public readonly NPath DataFolder;

		public readonly NPath SymbolsFolder;

		public readonly NPath MetadataFolder;

		public readonly NPath ExecutableAssembliesFolder;

		public readonly string EntryAssemblyName;

		public readonly NPath[] ExtraTypesFiles;

		public readonly RuntimeProfile Profile;

		public readonly ReadOnlyCollection<NPath> Assemblies;

		public readonly ReadOnlyCollection<NPath> SearchDirectories;

		public readonly int MaximumRecursiveGenericDepth;

		public readonly string AssemblyMethod;

		public readonly int IncrementalGCTimeSlice;

		public readonly ReadingMode CecilReadingMode;

		public readonly int JobCount;

		public readonly string[] DebugAssemblyName;

		public AssemblyConversionInputData(NPath outputDir, NPath dataFolder, NPath symbolsFolder, NPath metadataFolder, NPath executableAssembliesFolder, string entryAssemblyName, NPath[] extraTypesFiles, RuntimeProfile profile, ReadOnlyCollection<NPath> assemblies, ReadOnlyCollection<NPath> searchDirectories, int maximumRecursiveGenericDepth, string assemblyMethod, int incrementalGcTimeSlice, ReadingMode cecilReadingMode, int jobCount, string[] debugAssemblyName)
		{
			OutputDir = outputDir?.EnsureDirectoryExists().DeleteContents();
			DataFolder = dataFolder;
			SymbolsFolder = symbolsFolder;
			MetadataFolder = metadataFolder;
			ExecutableAssembliesFolder = executableAssembliesFolder;
			EntryAssemblyName = entryAssemblyName;
			ExtraTypesFiles = extraTypesFiles;
			Profile = profile;
			Assemblies = assemblies;
			SearchDirectories = searchDirectories;
			MaximumRecursiveGenericDepth = maximumRecursiveGenericDepth;
			AssemblyMethod = assemblyMethod;
			IncrementalGCTimeSlice = incrementalGcTimeSlice;
			CecilReadingMode = cecilReadingMode;
			JobCount = jobCount;
			DebugAssemblyName = debugAssemblyName;
		}
	}
}
