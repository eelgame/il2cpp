using System.Collections.Generic;
using System.Linq;
using NiceIO;
using Unity.IL2CPP.Building.BuildDescriptions;

namespace Unity.IL2CPP.Building.Platforms
{
	internal class LuminUnitTestPlusPlusBuildDescription : LuminCppRunnerBuildDescription
	{
		private readonly UnitTestPlusPlusBuildDescription _mUnitTestPlusPlusBuildDescription;

		public LuminUnitTestPlusPlusBuildDescription(UnitTestPlusPlusBuildDescription other)
			: base(other)
		{
			_mUnitTestPlusPlusBuildDescription = other;
		}

		public override IEnumerable<string> AdditionalDefinesFor(NPath sourceFile)
		{
			return base.AdditionalDefinesFor(sourceFile).Concat(_mUnitTestPlusPlusBuildDescription.AdditionalDefinesFor(sourceFile));
		}

		protected override IEnumerable<CppCompilationInstruction> LibIL2CPPCompileInstructions()
		{
			foreach (CppCompilationInstruction item in _mUnitTestPlusPlusBuildDescription.UnitTestPlusPlusLibIL2CPPCompileInstructions())
			{
				item.IncludePaths = item.IncludePaths.Concat(base.LibIL2CPPIncludeDirs);
				yield return item;
			}
		}
	}
}
