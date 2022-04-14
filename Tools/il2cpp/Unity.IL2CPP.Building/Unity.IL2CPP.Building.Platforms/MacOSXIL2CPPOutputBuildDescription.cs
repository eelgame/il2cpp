using System.Collections.Generic;
using System.Linq;
using NiceIO;
using Unity.IL2CPP.Building.BuildDescriptions;

namespace Unity.IL2CPP.Building.Platforms
{
	internal class MacOSXIL2CPPOutputBuildDescription : IL2CPPOutputBuildDescription
	{
		public override NPath PchCSourceFile => base.PchDirectory.Combine("pch-c.c");

		public override NPath PchCppSourceFile => base.PchDirectory.Combine("pch-cpp.cpp");

		public override IEnumerable<string> AdditionalLinkerFlags
		{
			get
			{
				foreach (string additionalLinkerFlag in base.AdditionalLinkerFlags)
				{
					yield return additionalLinkerFlag;
				}
				yield return "-liconv";
				yield return "-framework";
				yield return "Foundation";
				yield return "-framework";
				yield return "Security";
				yield return "-framework";
				yield return "CoreFoundation";
			}
		}

		public MacOSXIL2CPPOutputBuildDescription(IL2CPPOutputBuildDescription buildDescription)
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
