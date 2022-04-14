using System;
using Mono.Cecil;
using Unity.IL2CPP.Contexts.Components;
using Unity.IL2CPP.Contexts.Forking;

namespace Unity.IL2CPP.AssemblyConversion.InProcessPerAssembly
{
	public static class PerAssemblyUtilities
	{
		public static OverrideObjects CreateOverrideObjectsForPartial(string name, string cleanName)
		{
			return new OverrideObjects(new PathFactoryComponent(name));
		}

		public static OverrideObjects CreateOverrideObjectsForFull(Func<TypeReference, bool> shouldIncludeDefinition, string name, string cleanName)
		{
			return new OverrideObjects(new PathFactoryComponent(name), new ContextScopeServiceComponent(shouldIncludeDefinition, cleanName));
		}
	}
}
