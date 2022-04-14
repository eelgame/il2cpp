using System.Collections.Generic;
using Mono.Cecil;
using Unity.IL2CPP.GenericsCollection.CodeFlow.GraphGenerationData;
using Unity.IL2CPP.GenericsCollection.CodeFlow.GraphGenerationSteps;
using Unity.MiniProfiling;

namespace Unity.IL2CPP.GenericsCollection.CodeFlow
{
	public class GenericCodeFlowGraphGenerator
	{
		public static GenericCodeFlowGraph Generate(ref InputData inputData, IEnumerable<AssemblyDefinition> assemblies)
		{
			using (MiniProfiler.Section("GenericCodeFlowGraphGenerator.Generate"))
			{
				using (MiniProfiler.Section("AddMethodsToDefinitionsOfInterest"))
				{
					AddMethodsToDefinitionsOfInterestStep.Run(inputData.DefinitionsOfInterest);
				}
				GraphGenerationContext context = new GraphGenerationContext(ref inputData, assemblies);
				using (MiniProfiler.Section("GenerateFullGraph"))
				{
					FullGraphGenerationStep.Run(ref context);
				}
				Dictionary<IMemberDefinition, List<int>> dictionary = new Dictionary<IMemberDefinition, List<int>>();
				using (MiniProfiler.Section("BuildTypeDependencyLookupMap"))
				{
					BuildDependencyLookupMapStep.Run(dictionary, context.TypeDependencies);
				}
				using (MiniProfiler.Section("BuildMethodDependencyLookupMap"))
				{
					BuildDependencyLookupMapStep.Run(dictionary, context.MethodDependencies);
				}
				using (MiniProfiler.Section("MarkNeededNodes"))
				{
					MarkNeededNodesStep.Run(ref context, dictionary);
				}
				Dictionary<TypeDefinition, int> typeIndices;
				using (MiniProfiler.Section("CalculateTypeIndices"))
				{
					typeIndices = CalculateResultIndicesStep.Run(context.TypeNodes);
				}
				Dictionary<MethodDefinition, int> methodIndices;
				using (MiniProfiler.Section("CalculateMethodIndices"))
				{
					methodIndices = CalculateResultIndicesStep.Run(context.MethodNodes);
				}
				using (MiniProfiler.Section("GenerateFinalGraph"))
				{
					return GenerateFinalGraphStep.Run(ref context, typeIndices, methodIndices);
				}
			}
		}
	}
}
