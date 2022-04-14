using System.Collections.Generic;
using NiceIO;
using Unity.IL2CPP.Common;

namespace Unity.IL2CPP.Building.BuildDescriptions.Mono
{
	public class WindowsDebuggerMonoSourceFileList : UnityMonoSourceFileList
	{
		public override IEnumerable<NPath> GetEGLibSourceFiles(Architecture architecture)
		{
			foreach (NPath eGLibSourceFile in base.GetEGLibSourceFiles(architecture))
			{
				yield return eGLibSourceFile;
			}
			yield return MonoSourceFileList.MonoSourceDir.Combine("mono/eglib/gunicode-win32.c");
		}

		public override IEnumerable<NPath> GetUtilsSourceFiles(Architecture architecture)
		{
			foreach (NPath utilsSourceFile in base.GetUtilsSourceFiles(architecture))
			{
				yield return utilsSourceFile;
			}
			yield return MonoSourceFileList.MonoSourceDir.Combine("mono/utils/mono-os-wait-win32.c");
		}
	}
}
