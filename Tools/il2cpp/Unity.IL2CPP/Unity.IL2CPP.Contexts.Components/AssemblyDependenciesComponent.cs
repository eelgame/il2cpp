using System;
using System.Collections.Generic;
using Mono.Cecil;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts.Components.Base;
using Unity.IL2CPP.Contexts.Services;

namespace Unity.IL2CPP.Contexts.Components
{
	public class AssemblyDependenciesComponent : ServiceComponentBase<IAssemblyDependencyResults, AssemblyDependenciesComponent>, IAssemblyDependencyResults
	{
		private readonly Unity.IL2CPP.Common.AssemblyDependenciesComponent _internalAssemblyDependencies;

		public Unity.IL2CPP.Common.AssemblyDependenciesComponent InternalComponent => _internalAssemblyDependencies;

		public AssemblyDependenciesComponent()
		{
			_internalAssemblyDependencies = new Unity.IL2CPP.Common.AssemblyDependenciesComponent();
		}

		public IEnumerable<AssemblyDefinition> GetReferencedAssembliesFor(AssemblyDefinition assembly)
		{
			return _internalAssemblyDependencies.GetReferencedAssembliesFor(assembly);
		}

		IEnumerable<AssemblyDefinition> IAssemblyDependencyResults.GetReferencedAssembliesFor(AssemblyDefinition assembly)
		{
			if (!_internalAssemblyDependencies.TryGetReferences(assembly, out var references))
			{
				throw new InvalidOperationException("References have not been collected for `" + assembly.Name.Name + "`");
			}
			return references;
		}

		protected override AssemblyDependenciesComponent ThisAsFull()
		{
			return this;
		}

		protected override IAssemblyDependencyResults ThisAsRead()
		{
			return this;
		}
	}
}
