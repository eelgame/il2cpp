using System.Collections.Generic;
using NiceIO;
using Unity.IL2CPP.Common;

namespace Unity.IL2CPP.Building.BuildDescriptions.Mono
{
	public class PosixMonoSourceFileList : MonoSourceFileList
	{
		public override IEnumerable<NPath> GetMetadataSourceFiles(Architecture architecture)
		{
			foreach (NPath metadataSourceFile in base.GetMetadataSourceFiles(architecture))
			{
				yield return metadataSourceFile;
			}
			yield return MonoSourceFileList.MonoSourceDir.Combine("mono/metadata/console-unix.c");
			yield return MonoSourceFileList.MonoSourceDir.Combine("mono/metadata/mono-route.c");
			yield return MonoSourceFileList.MonoSourceDir.Combine("mono/metadata/w32error-unix.c");
			yield return MonoSourceFileList.MonoSourceDir.Combine("mono/metadata/w32event-unix.c");
			yield return MonoSourceFileList.MonoSourceDir.Combine("mono/metadata/w32file-unix-glob.c");
			yield return MonoSourceFileList.MonoSourceDir.Combine("mono/metadata/w32file-unix.c");
			yield return MonoSourceFileList.MonoSourceDir.Combine("mono/metadata/w32mutex-unix.c");
			yield return MonoSourceFileList.MonoSourceDir.Combine("mono/metadata/w32process-unix.c");
			yield return MonoSourceFileList.MonoSourceDir.Combine("mono/metadata/w32semaphore-unix.c");
			yield return MonoSourceFileList.MonoSourceDir.Combine("mono/metadata/w32socket-unix.c");
		}

		public override IEnumerable<NPath> GetUtilsSourceFiles(Architecture architecture)
		{
			foreach (NPath utilsSourceFile in base.GetUtilsSourceFiles(architecture))
			{
				yield return utilsSourceFile;
			}
			yield return MonoSourceFileList.MonoSourceDir.Combine("mono/utils/mono-dl-posix.c");
			yield return MonoSourceFileList.MonoSourceDir.Combine("mono/utils/mono-log-posix.c");
			yield return MonoSourceFileList.MonoSourceDir.Combine("mono/utils/mono-threads-posix.c");
			yield return MonoSourceFileList.MonoSourceDir.Combine("mono/utils/mono-threads-posix-signals.c");
			yield return MonoSourceFileList.MonoSourceDir.Combine("mono/utils/os-event-unix.c");
			yield return MonoSourceFileList.MonoSourceDir.Combine("mono/utils/networking-posix.c");
		}
	}
}
