using System.Collections.Generic;
using System.Linq;
using NiceIO;
using Unity.IL2CPP.Building.BuildDescriptions;

namespace Unity.IL2CPP.Building.Platforms
{
	internal class MacOSXUnitTestPlusPlusBuildDescription : UnitTestPlusPlusBuildDescription
	{
		public MacOSXUnitTestPlusPlusBuildDescription(UnitTestPlusPlusBuildDescription other)
			: base(other)
		{
		}

		public override IEnumerable<NPath> SourceFilesIn(params NPath[] foldersToGlob)
		{
			return from f in foldersToGlob.SelectMany((NPath d) => d.Files("*.*", recurse: true))
				where f.HasExtension("c", "cpp", "mm")
				select f;
		}
	}
}
