using System.Collections.Generic;
using System.Linq;
using NiceIO;

namespace Unity.IL2CPP.Building
{
	public class CppCompilationInstruction
	{
		public NPath SourceFile { get; set; }

		public IEnumerable<string> Defines { get; set; }

		public IEnumerable<NPath> IncludePaths { get; set; }

		public IEnumerable<NPath> LumpPaths { get; set; }

		public IEnumerable<string> CompilerFlags { get; set; }

		public NPath CacheDirectory { get; set; }

		public bool TreatWarningsAsErrors { get; set; }

		public CppCompilationInstruction()
		{
			Defines = Enumerable.Empty<string>();
			IncludePaths = Enumerable.Empty<NPath>();
			LumpPaths = Enumerable.Empty<NPath>();
			CompilerFlags = Enumerable.Empty<string>();
			TreatWarningsAsErrors = true;
		}
	}
}
