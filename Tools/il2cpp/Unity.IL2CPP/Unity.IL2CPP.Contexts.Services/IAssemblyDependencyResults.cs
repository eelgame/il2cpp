using System.Collections.Generic;
using Mono.Cecil;

namespace Unity.IL2CPP.Contexts.Services
{
	public interface IAssemblyDependencyResults
	{
		IEnumerable<AssemblyDefinition> GetReferencedAssembliesFor(AssemblyDefinition assembly);
	}
}
