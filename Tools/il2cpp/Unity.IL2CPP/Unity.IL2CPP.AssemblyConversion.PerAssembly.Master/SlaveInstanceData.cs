using System.Diagnostics;
using Mono.Cecil;

namespace Unity.IL2CPP.AssemblyConversion.PerAssembly.Master
{
	[DebuggerDisplay("{PrimaryAssembly.Name.Name}")]
	internal class SlaveInstanceData
	{
		public AssemblyDefinition PrimaryAssembly;

		public string[] Arguments;
	}
}
