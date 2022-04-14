using System.Collections.Generic;
using System.Linq;
using NiceIO;
using Unity.MiniProfiling;

namespace Unity.IL2CPP.Building.Hashing
{
	internal class LumpHashProvider : FileHashProvider
	{
		protected override IEnumerable<NPath> DirectoriesToInitializeForInstruction(CppCompilationInstruction ins)
		{
			return ins.LumpPaths;
		}

		public string HashForAllFilesUsedForLumping(CppCompilationInstruction cppCompilationInstruction, string[] fileExtensions)
		{
			return string.Concat(ParallelFor.RunWithResult(cppCompilationInstruction.LumpPaths.Where((NPath lumpPath) => lumpPath.DirectoryExists()).ToSortedCollection().ToArray(), delegate(NPath dir)
			{
				using (MiniProfiler.Section("LumpHashProvider.HashForAllFilesUsedForLumping", dir))
				{
					return HashOfAllFilesWithProviderExtensionInDirectory(dir, fileExtensions);
				}
			}, 2));
		}
	}
}
