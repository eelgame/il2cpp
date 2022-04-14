using System.Collections.Generic;
using NiceIO;
using Unity.IL2CPP.Common;

namespace Unity.IL2CPP.Building.BuildDescriptions.Mono
{
	public class WindowsSharedMonoSourceFileList : MonoSourceFileList
	{
		public override IEnumerable<NPath> GetMetadataSourceFiles(Architecture architecture)
		{
			foreach (NPath metadataSourceFile in base.GetMetadataSourceFiles(architecture))
			{
				yield return metadataSourceFile;
			}
			yield return MonoSourceFileList.MonoSourceDir.Combine("mono/metadata/console-win32.c");
			yield return MonoSourceFileList.MonoSourceDir.Combine("mono/metadata/w32error-win32.c");
			yield return MonoSourceFileList.MonoSourceDir.Combine("mono/metadata/w32event-win32.c");
			yield return MonoSourceFileList.MonoSourceDir.Combine("mono/metadata/w32file-win32.c");
			yield return MonoSourceFileList.MonoSourceDir.Combine("mono/metadata/w32mutex-win32.c");
			yield return MonoSourceFileList.MonoSourceDir.Combine("mono/metadata/w32process-win32.c");
			yield return MonoSourceFileList.MonoSourceDir.Combine("mono/metadata/w32semaphore-win32.c");
			yield return MonoSourceFileList.MonoSourceDir.Combine("mono/metadata/w32socket-win32.c");
		}

		public override IEnumerable<NPath> GetUtilsSourceFiles(Architecture architecture)
		{
			foreach (NPath utilsSourceFile in base.GetUtilsSourceFiles(architecture))
			{
				yield return utilsSourceFile;
			}
			yield return MonoSourceFileList.MonoSourceDir.Combine("mono/utils/mono-dl-windows.c");
			yield return MonoSourceFileList.MonoSourceDir.Combine("mono/utils/mono-log-windows.c");
			yield return MonoSourceFileList.MonoSourceDir.Combine("mono/utils/mono-os-wait-win32.c");
			yield return MonoSourceFileList.MonoSourceDir.Combine("mono/utils/mono-threads-windows.c");
			yield return MonoSourceFileList.MonoSourceDir.Combine("mono/utils/os-event-win32.c");
			yield return MonoSourceFileList.MonoSourceDir.Combine("mono/utils/networking-posix.c");
			yield return MonoSourceFileList.MonoSourceDir.Combine("mono/utils/networking-windows.c");
		}
	}
}
