using System.Collections.Generic;
using System.Linq;
using NiceIO;
using Unity.IL2CPP.Common;

namespace Unity.IL2CPP.Building.BuildDescriptions.Mono
{
	public class UnityMonoSourceFileList : MonoSourceFileList
	{
		public override IEnumerable<NPath> GetUtilsSourceFiles(Architecture architecture)
		{
			foreach (NPath utilsSourceFile in base.GetUtilsSourceFiles(architecture))
			{
				yield return utilsSourceFile;
			}
			yield return MonoSourceFileList.MonoSourceDir.Combine("mono/utils/mono-dl-unity.c");
			yield return MonoSourceFileList.MonoSourceDir.Combine("mono/utils/mono-log-unity.c");
			yield return MonoSourceFileList.MonoSourceDir.Combine("mono/utils/mono-threads-unity.c");
			yield return MonoSourceFileList.MonoSourceDir.Combine("mono/utils/networking-unity.c");
			yield return MonoSourceFileList.MonoSourceDir.Combine("mono/utils/os-event-unity.c");
		}

		public override IEnumerable<NPath> GetMetadataDebuggerSourceFiles(Architecture architecture)
		{
			foreach (NPath metadataDebuggerSourceFile in base.GetMetadataDebuggerSourceFiles(architecture))
			{
				yield return metadataDebuggerSourceFile;
			}
			yield return MonoSourceFileList.MonoSourceDir.Combine("mono/metadata/console-unity.c");
			yield return MonoSourceFileList.MonoSourceDir.Combine("mono/metadata/file-mmap-unity.c");
			yield return MonoSourceFileList.MonoSourceDir.Combine("mono/metadata/w32error-unity.c");
			yield return MonoSourceFileList.MonoSourceDir.Combine("mono/metadata/w32event-unity.c");
			yield return MonoSourceFileList.MonoSourceDir.Combine("mono/metadata/w32file-unity.c");
			yield return MonoSourceFileList.MonoSourceDir.Combine("mono/metadata/w32mutex-unity.c");
			yield return MonoSourceFileList.MonoSourceDir.Combine("mono/metadata/w32process-unity.c");
			yield return MonoSourceFileList.MonoSourceDir.Combine("mono/metadata/w32semaphore-unity.c");
			yield return MonoSourceFileList.MonoSourceDir.Combine("mono/metadata/w32socket-unity.c");
		}

		public override IEnumerable<NPath> GetMetadataSourceFiles(Architecture architecture)
		{
			return Enumerable.Empty<NPath>();
		}
	}
}
