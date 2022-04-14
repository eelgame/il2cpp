using System.Collections.Generic;
using Mono.Cecil;

namespace Unity.IL2CPP.GenericsCollection.CodeFlow.GraphGenerationData
{
	internal struct GraphGenerationContext
	{
		private readonly InputData _inputData;

		public readonly IEnumerable<AssemblyDefinition> Assemblies;

		public readonly List<StagingNode<MethodDefinition>> MethodNodes;

		public readonly List<StagingNode<TypeDefinition>> TypeNodes;

		public readonly List<StagingDependency<MethodReferenceDefinitionPair>> MethodDependencies;

		public readonly List<StagingDependency<TypeReferenceDefinitionPair>> TypeDependencies;

		public HashSet<IMemberDefinition> DefinitionsOfInterest => _inputData.DefinitionsOfInterest;

		public Dictionary<IMemberDefinition, List<GenericInstanceType>> ImplicitDependencies => _inputData.ImplicitDependencies;

		public bool ArraysAreOfInterest => _inputData.ArraysAreOfInterest;

		public GraphGenerationContext(ref InputData inputData, IEnumerable<AssemblyDefinition> assemblies)
		{
			_inputData = inputData;
			Assemblies = assemblies;
			MethodNodes = new List<StagingNode<MethodDefinition>>();
			TypeNodes = new List<StagingNode<TypeDefinition>>();
			MethodDependencies = new List<StagingDependency<MethodReferenceDefinitionPair>>();
			TypeDependencies = new List<StagingDependency<TypeReferenceDefinitionPair>>();
		}
	}
}
