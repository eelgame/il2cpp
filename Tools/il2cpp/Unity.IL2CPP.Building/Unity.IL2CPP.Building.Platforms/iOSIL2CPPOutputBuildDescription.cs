using System.Collections.Generic;
using System.Linq;
using NiceIO;
using Unity.IL2CPP.Building.BuildDescriptions;

namespace Unity.IL2CPP.Building.Platforms
{
	internal class iOSIL2CPPOutputBuildDescription : IL2CPPOutputBuildDescription
	{
		public override IEnumerable<string> AdditionalLinkerFlags
		{
			get
			{
				foreach (string additionalLinkerFlag in base.AdditionalLinkerFlags)
				{
					yield return additionalLinkerFlag;
				}
				yield return "-liconv -framework Foundation";
			}
		}

		public iOSIL2CPPOutputBuildDescription(IL2CPPOutputBuildDescription buildDescription)
			: base(buildDescription)
		{
		}

		public override IEnumerable<NPath> SourceFilesIn(params NPath[] foldersToGlob)
		{
			IEnumerable<NPath> first = base.SourceFilesIn(foldersToGlob);
			IEnumerable<NPath> second = from f in foldersToGlob.SelectMany((NPath d) => d.Files("*.m*", recurse: true))
				where f.HasExtension("mm") || f.HasExtension("m")
				select f;
			return first.Concat(second);
		}
	}
}
