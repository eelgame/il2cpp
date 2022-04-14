using System.Collections.Generic;
using System.Linq;
using NiceIO;
using Unity.MiniProfiling;

namespace Unity.IL2CPP.Building.Hashing
{
	public class HeaderFileHashProvider : FileHashProvider
	{
		protected override IEnumerable<NPath> DirectoriesToInitializeForInstruction(CppCompilationInstruction ins)
		{
			return ins.IncludePaths;
		}

		public string HashForAllHeaderFilesReachableByFilesIn(CppCompilationInstruction cppCompilationInstruction, string[] fileExtensions)
		{
			return string.Concat(ParallelFor.RunWithResult(cppCompilationInstruction.IncludePaths.Where((NPath includePath) => includePath.DirectoryExists()).ToSortedCollection().ToArray(), delegate(NPath dir)
			{
				using (MiniProfiler.Section("HeaderFileHashProvider.HashForAllHeaderFilesReachableByFilesIn", dir))
				{
					return HashOfAllFilesWithProviderExtensionInDirectory(dir, fileExtensions);
				}
			}, 2));
		}

		public string HashForAllIncludableFilesInDirectories(IEnumerable<NPath> directories, string[] fileExtensions)
		{
			return string.Concat(ParallelFor.RunWithResult(directories.Where((NPath d) => d.DirectoryExists()).ToSortedCollection().ToArray(), delegate(NPath dir)
			{
				using (MiniProfiler.Section("HeaderFileHashProvider.HashForAllIncludableFilesInDirectories", dir))
				{
					return HashOfAllFilesWithProviderExtensionInDirectory(dir, fileExtensions);
				}
			}, 2));
		}
	}
}
