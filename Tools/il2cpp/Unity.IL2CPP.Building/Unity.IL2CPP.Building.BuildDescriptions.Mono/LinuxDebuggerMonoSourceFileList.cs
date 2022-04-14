using System.Collections.Generic;
using NiceIO;
using Unity.IL2CPP.Common;

namespace Unity.IL2CPP.Building.BuildDescriptions.Mono
{
	public class LinuxDebuggerMonoSourceFileList : UnityMonoSourceFileList
	{
		public override IEnumerable<NPath> GetUtilsSourceFiles(Architecture architecture)
		{
			foreach (NPath utilsSourceFile in base.GetUtilsSourceFiles(architecture))
			{
				yield return utilsSourceFile;
			}
		}
	}
}
