using System.Collections.Generic;
using System.Linq;
using NiceIO;
using Unity.IL2CPP.Building.BuildDescriptions;
using Unity.IL2CPP.Building.ToolChains;

namespace Unity.IL2CPP.Building.Platforms
{
	internal class LuminCppRunnerBuildDescription : IL2CPPOutputBuildDescription
	{
		public LuminCppRunnerBuildDescription(IL2CPPOutputBuildDescription buildDescription)
			: base(buildDescription)
		{
		}

		public override IEnumerable<NPath> AdditionalIncludePathsFor(NPath sourceFile)
		{
			return base.AdditionalIncludePathsFor(sourceFile).Concat(LuminSDK.PlatformIncludes);
		}
	}
}
