using System.Collections.Generic;
using NiceIO;
using Unity.IL2CPP.Common;

namespace Unity.IL2CPP.Building.BuildDescriptions.Mono
{
	public class LinuxMonoSourceFileList : PosixMonoSourceFileList
	{
		public override IEnumerable<NPath> GetMetadataSourceFiles(Architecture architecture)
		{
			foreach (NPath metadataSourceFile in base.GetMetadataSourceFiles(architecture))
			{
				yield return metadataSourceFile;
			}
			yield return MonoSourceFileList.MonoSourceDir.Combine("mono/metadata/w32process-unix-default.c");
			yield return MonoSourceFileList.MonoSourceDir.Combine("support/libm/complex.c");
		}

		public override IEnumerable<NPath> GetUtilsSourceFiles(Architecture architecture)
		{
			foreach (NPath utilsSourceFile in base.GetUtilsSourceFiles(architecture))
			{
				yield return utilsSourceFile;
			}
		}
	}
}
