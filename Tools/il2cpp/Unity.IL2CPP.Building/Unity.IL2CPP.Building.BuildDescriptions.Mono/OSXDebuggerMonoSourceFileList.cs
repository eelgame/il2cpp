using System.Collections.Generic;
using NiceIO;
using Unity.IL2CPP.Common;

namespace Unity.IL2CPP.Building.BuildDescriptions.Mono
{
	public class OSXDebuggerMonoSourceFileList : UnityMonoSourceFileList
	{
		public override IEnumerable<NPath> GetUtilsSourceFiles(Architecture architecture)
		{
			foreach (NPath utilsSourceFile in base.GetUtilsSourceFiles(architecture))
			{
				yield return utilsSourceFile;
			}
			if (architecture is x64Architecture)
			{
				yield return MonoSourceFileList.MonoSourceDir.Combine("mono/utils/mach-support-amd64.c");
			}
			if (architecture is ARM64Architecture)
			{
				yield return MonoSourceFileList.MonoSourceDir.Combine("mono/utils/mach-support-arm64.c");
			}
		}
	}
}
