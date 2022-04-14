using System.Collections.Generic;
using NiceIO;
using Unity.IL2CPP.Common;

namespace Unity.IL2CPP.Building.BuildDescriptions.Mono
{
	public class OSXMonoSourceFileList : PosixMonoSourceFileList
	{
		public override IEnumerable<NPath> GetMetadataSourceFiles(Architecture architecture)
		{
			foreach (NPath metadataSourceFile in base.GetMetadataSourceFiles(architecture))
			{
				yield return metadataSourceFile;
			}
			yield return MonoSourceFileList.MonoSourceDir.Combine("mono/metadata/w32process-unix-osx.c");
		}

		public override IEnumerable<NPath> GetUtilsSourceFiles(Architecture architecture)
		{
			foreach (NPath utilsSourceFile in base.GetUtilsSourceFiles(architecture))
			{
				yield return utilsSourceFile;
			}
			yield return MonoSourceFileList.MonoSourceDir.Combine("mono/utils/mach-support.c");
			yield return MonoSourceFileList.MonoSourceDir.Combine("mono/utils/mono-dl-darwin.c");
			yield return MonoSourceFileList.MonoSourceDir.Combine("mono/utils/mono-log-darwin.c");
			yield return MonoSourceFileList.MonoSourceDir.Combine("mono/utils/mono-threads-mach.c");
			yield return MonoSourceFileList.MonoSourceDir.Combine("mono/utils/mono-threads-mach-helper.c");
			if (architecture is x64Architecture)
			{
				yield return MonoSourceFileList.MonoSourceDir.Combine("mono/utils/mach-support-amd64.c");
			}
			if (architecture is x86Architecture)
			{
				yield return MonoSourceFileList.MonoSourceDir.Combine("mono/utils/mach-support-x86.c");
			}
		}
	}
}
