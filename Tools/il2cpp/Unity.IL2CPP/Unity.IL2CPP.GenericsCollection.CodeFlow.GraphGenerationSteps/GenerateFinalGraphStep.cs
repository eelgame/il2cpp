using System.Collections.Generic;
using Mono.Cecil;
using Unity.IL2CPP.GenericsCollection.CodeFlow.GraphGenerationData;

namespace Unity.IL2CPP.GenericsCollection.CodeFlow.GraphGenerationSteps
{
	internal static class GenerateFinalGraphStep
	{
		public static GenericCodeFlowGraph Run(ref GraphGenerationContext context, Dictionary<TypeDefinition, int> typeIndices, Dictionary<MethodDefinition, int> methodIndices)
		{
			List<TypeDependency> list = new List<TypeDependency>();
			List<MethodDependency> list2 = new List<MethodDependency>();
			Node<TypeDefinition>[] array = new Node<TypeDefinition>[typeIndices.Count];
			Node<MethodDefinition>[] array2 = new Node<MethodDefinition>[methodIndices.Count];
			CollectNeededNodes(ref context, context.TypeNodes, typeIndices, methodIndices, array, list, list2);
			CollectNeededNodes(ref context, context.MethodNodes, typeIndices, methodIndices, array2, list, list2);
			return new GenericCodeFlowGraph(context.Assemblies, array2, array, list2, list, typeIndices);
		}

		private static void CollectNeededNodes<T>(ref GraphGenerationContext context, List<StagingNode<T>> inNodes, Dictionary<TypeDefinition, int> typeIndices, Dictionary<MethodDefinition, int> methodIndices, Node<T>[] outNodes, List<TypeDependency> outTypeDependencies, List<MethodDependency> outMethodDependencies)
		{
			HashSet<IMemberDefinition> definitionsOfInterest = context.DefinitionsOfInterest;
			List<StagingDependency<TypeReferenceDefinitionPair>> typeDependencies = context.TypeDependencies;
			List<StagingDependency<MethodReferenceDefinitionPair>> methodDependencies = context.MethodDependencies;
			int count = inNodes.Count;
			int num = 0;
			for (int i = 0; i < count; i++)
			{
				StagingNode<T> stagingNode = inNodes[i];
				if (stagingNode.IsNeeded)
				{
					int count2 = outTypeDependencies.Count;
					int count3 = outMethodDependencies.Count;
					CollectTypeDependencies(definitionsOfInterest, typeDependencies, stagingNode.TypeDependenciesStartIndex, stagingNode.TypeDependenciesEndIndex, typeIndices, outTypeDependencies);
					CollectMethodDependencies(definitionsOfInterest, methodDependencies, stagingNode.MethodDependenciesStartIndex, stagingNode.MethodDependenciesEndIndex, methodIndices, outMethodDependencies);
					int count4 = outTypeDependencies.Count;
					int count5 = outMethodDependencies.Count;
					outNodes[num++] = new Node<T>(stagingNode.Item, count3, count5, count2, count4);
				}
			}
		}

		private static void CollectTypeDependencies(HashSet<IMemberDefinition> definitionsOfInterest, List<StagingDependency<TypeReferenceDefinitionPair>> inDependencies, int startIndex, int endIndex, Dictionary<TypeDefinition, int> typeIndices, List<TypeDependency> outDependencies)
		{
			_ = inDependencies.Count;
			for (int i = startIndex; i < endIndex; i++)
			{
				StagingDependency<TypeReferenceDefinitionPair> stagingDependency = inDependencies[i];
				if (stagingDependency.IsNeeded)
				{
					TypeReferenceDefinitionPair dependency = stagingDependency.Dependency;
					TypeDependencyKind typeDependencyKind = dependency.Kind;
					if (definitionsOfInterest.Contains(dependency.Definition))
					{
						typeDependencyKind |= TypeDependencyKind.IsOfInterest;
					}
					outDependencies.Add(new TypeDependency(dependency.Reference, GetDefinitionIndex(typeIndices, dependency.Definition), typeDependencyKind));
				}
			}
		}

		private static void CollectMethodDependencies(HashSet<IMemberDefinition> definitionsOfInterest, List<StagingDependency<MethodReferenceDefinitionPair>> inDependencies, int startIndex, int endIndex, Dictionary<MethodDefinition, int> methodIndices, List<MethodDependency> outDependencies)
		{
			_ = inDependencies.Count;
			for (int i = startIndex; i < endIndex; i++)
			{
				StagingDependency<MethodReferenceDefinitionPair> stagingDependency = inDependencies[i];
				if (stagingDependency.IsNeeded)
				{
					MethodReferenceDefinitionPair dependency = stagingDependency.Dependency;
					bool isOfInterest = definitionsOfInterest.Contains(dependency.Definition);
					outDependencies.Add(new MethodDependency(dependency.Reference, GetDefinitionIndex(methodIndices, dependency.Definition), isOfInterest));
				}
			}
		}

		private static int GetDefinitionIndex<T>(Dictionary<T, int> indices, T definition)
		{
			if (definition == null)
			{
				return -1;
			}
			if (indices.TryGetValue(definition, out var value))
			{
				return value;
			}
			return -1;
		}
	}
}
