using NiceIO;

namespace Unity.IL2CPP.Building.ToolChains
{
	public class MsvcToolChainContext : CppToolChainContext
	{
		public NPath ManifestPath { get; set; }

		public NPath ModuleDefinitionPath { get; set; }

		public bool TreatWarningsAsErrors { get; set; }

		public bool UseDependenciesToolChain { get; set; }
	}
}
