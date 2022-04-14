using System.Collections.Generic;
using NiceIO;

namespace Unity.IL2CPP.Building.ToolChains
{
	public class CppToolChainContext
	{
		private readonly List<CppCompilationInstruction> _extraCompileInstructions = new List<CppCompilationInstruction>();

		private readonly List<NPath> _extraIncludeDirectories = new List<NPath>();

		public IEnumerable<CppCompilationInstruction> ExtraCompileInstructions => _extraCompileInstructions;

		public IEnumerable<NPath> ExtraIncludeDirectories => _extraIncludeDirectories;

		public void AddCompileInstructions(IEnumerable<CppCompilationInstruction> compileInstructions)
		{
			_extraCompileInstructions.AddRange(compileInstructions);
		}

		public void AddIncludeDirectory(NPath includeDirectory)
		{
			_extraIncludeDirectories.Add(includeDirectory);
		}
	}
}
