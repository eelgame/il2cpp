using System.Collections.Generic;
using System.Linq;
using NiceIO;
using Unity.IL2CPP.Building.BuildDescriptions;

namespace Unity.IL2CPP.Building.Platforms
{
	internal class WindowsGamesUnitTestPlusPlusBuildDescription : WindowsIL2CPPOutputBuildDescription
	{
		private UnitTestPlusPlusBuildDescription m_UnitTestPlusPlusBuildDescription;

		public WindowsGamesUnitTestPlusPlusBuildDescription(UnitTestPlusPlusBuildDescription other)
			: base(other)
		{
			m_UnitTestPlusPlusBuildDescription = other;
		}

		public override IEnumerable<string> AdditionalDefinesFor(NPath sourceFile)
		{
			return m_UnitTestPlusPlusBuildDescription.AdditionalDefinesFor(sourceFile);
		}

		protected override IEnumerable<CppCompilationInstruction> LibIL2CPPCompileInstructions()
		{
			foreach (CppCompilationInstruction item in m_UnitTestPlusPlusBuildDescription.UnitTestPlusPlusLibIL2CPPCompileInstructions())
			{
				item.IncludePaths = item.IncludePaths.Concat(base.LibIL2CPPIncludeDirs);
				yield return item;
			}
		}
	}
}
