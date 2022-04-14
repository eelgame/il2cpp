using System;
using System.Collections.Generic;
using Mono.Cecil;
using Unity.IL2CPP.GenericsCollection.CodeFlow.GraphGenerationData;

namespace Unity.IL2CPP.GenericsCollection.CodeFlow.GraphGenerationSteps
{
	internal static class MarkNeededNodesStep
	{
		public static void Run(ref GraphGenerationContext context, Dictionary<IMemberDefinition, List<int>> dependencyLookupMap)
		{
			List<StagingNode<TypeDefinition>> typeNodes = context.TypeNodes;
			List<StagingNode<MethodDefinition>> methodNodes = context.MethodNodes;
			List<StagingDependency<TypeReferenceDefinitionPair>> typeDependencies = context.TypeDependencies;
			List<StagingDependency<MethodReferenceDefinitionPair>> methodDependencies = context.MethodDependencies;
			List<int> list = new List<int>(typeNodes.Count);
			List<int> list2 = new List<int>(methodNodes.Count);
			AddNeededNodes(context.DefinitionsOfInterest, typeDependencies, list2, list);
			AddNeededNodes(context.DefinitionsOfInterest, methodDependencies, list2, list);
			while (list.Count > 0 || list2.Count > 0)
			{
				ProcessNeededNodes(typeNodes, list2, list2, list, dependencyLookupMap, typeDependencies);
				ProcessNeededNodes(methodNodes, list, list2, list, dependencyLookupMap, methodDependencies);
			}
		}

		private static void AddNeededNodes<T>(HashSet<IMemberDefinition> definitionsOfInterest, List<StagingDependency<T>> dependencies, List<int> typeNodesToProcess, List<int> methodNodesToProcess) where T : IHasDefinition, IEquatable<T>
		{
			int count = dependencies.Count;
			for (int i = 0; i < count; i++)
			{
				StagingDependency<T> value = dependencies[i];
				T dependency = value.Dependency;
				IMemberDefinition definition = dependency.GetDefinition();
				if (definition == null || definitionsOfInterest.Contains(definition))
				{
					value.IsNeeded = true;
					dependencies[i] = value;
					AddReferrerNode(typeNodesToProcess, methodNodesToProcess, value.ReferrerIndex);
				}
			}
		}

		private static void ProcessNeededNodes<T, TPair>(List<StagingNode<T>> nodes, List<int> nodesToProcess, List<int> typeNodesToProcess, List<int> methodNodesToProcess, Dictionary<IMemberDefinition, List<int>> dependencyLookupMap, List<StagingDependency<TPair>> dependencies) where T : IMemberDefinition where TPair : IEquatable<TPair>
		{
			int count = nodesToProcess.Count;
			if (count == 0)
			{
				return;
			}
			int index = nodesToProcess[count - 1];
			StagingNode<T> value = nodes[index];
			nodesToProcess.RemoveAt(count - 1);
			if (value.IsNeeded)
			{
				return;
			}
			value.IsNeeded = true;
			nodes[index] = value;
			if (dependencyLookupMap.TryGetValue(value.Item, out var value2))
			{
				int count2 = value2.Count;
				for (int i = 0; i < count2; i++)
				{
					int index2 = value2[i];
					StagingDependency<TPair> value3 = dependencies[index2];
					value3.IsNeeded = true;
					dependencies[index2] = value3;
					AddReferrerNode(typeNodesToProcess, methodNodesToProcess, value3.ReferrerIndex);
				}
			}
		}

		private static void AddReferrerNode(List<int> typeNodesToProcess, List<int> methodNodesToProcess, int referrerIndex)
		{
			if ((referrerIndex & 0x80000000u) != 0L)
			{
				typeNodesToProcess.Add(referrerIndex & 0x7FFFFFFF);
			}
			else
			{
				methodNodesToProcess.Add(referrerIndex);
			}
		}
	}
}
