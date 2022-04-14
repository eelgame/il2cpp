using System;
using System.Collections.Generic;
using Mono.Cecil;

namespace Unity.IL2CPP.GenericsCollection.CodeFlow.GraphGenerationSteps
{
	internal static class BuildDependencyLookupMapStep
	{
		public static void Run<TPair>(Dictionary<IMemberDefinition, List<int>> lookupMap, List<StagingDependency<TPair>> dependencies) where TPair : IHasDefinition, IEquatable<TPair>
		{
			int count = dependencies.Count;
			for (int i = 0; i < count; i++)
			{
				TPair dependency = dependencies[i].Dependency;
				IMemberDefinition definition = dependency.GetDefinition();
				if (definition != null)
				{
					if (!lookupMap.TryGetValue(definition, out var value))
					{
						lookupMap.Add(definition, value = new List<int>());
					}
					value.Add(i);
				}
			}
		}
	}
}
