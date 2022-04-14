using System.Collections.Generic;
using System.Linq;
using Unity.IL2CPP.Building.BuildDescriptions;

namespace Unity.IL2CPP.Building.Platforms
{
	internal class EmscriptenIL2CPPOutputBuildDescription : IL2CPPOutputBuildDescription
	{
		public EmscriptenIL2CPPOutputBuildDescription(IL2CPPOutputBuildDescription buildDescription)
			: base(buildDescription)
		{
		}

		protected override IEnumerable<string> BoehmDefines()
		{
			List<string> list = base.BoehmDefines().ToList();
			list.Remove("GC_THREADS=1");
			list.Remove("USE_MMAP=1");
			list.Remove("USE_MUNMAP=1");
			return list;
		}
	}
}
