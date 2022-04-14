using System.Collections.Generic;
using Unity.IL2CPP.Building.BuildDescriptions;

namespace Unity.IL2CPP.Building.Platforms
{
	internal class AndroidCppRunnerBuildDescription : IL2CPPOutputBuildDescription
	{
		public override IEnumerable<string> AdditionalCompilerFlags
		{
			get
			{
				foreach (string additionalCompilerFlag in base.AdditionalCompilerFlags)
				{
					yield return additionalCompilerFlag;
				}
				if (!OutputFile.HasExtension(_cppToolChain.DynamicLibraryExtension))
				{
					yield return "-fPIE";
				}
			}
		}

		public AndroidCppRunnerBuildDescription(IL2CPPOutputBuildDescription buildDescription)
			: base(buildDescription)
		{
		}
	}
}
