using System.Collections.Generic;
using Mono.Cecil;

namespace Unity.IL2CPP.GenericsCollection.CodeFlow.GraphGenerationData
{
	public struct InputData
	{
		public readonly HashSet<IMemberDefinition> DefinitionsOfInterest;

		public readonly Dictionary<IMemberDefinition, List<GenericInstanceType>> ImplicitDependencies;

		public readonly bool ArraysAreOfInterest;

		public InputData(HashSet<IMemberDefinition> definitionsOfInterest, Dictionary<IMemberDefinition, List<GenericInstanceType>> implicitDependencies, bool arraysAreOfInterest)
		{
			DefinitionsOfInterest = definitionsOfInterest;
			ImplicitDependencies = implicitDependencies;
			ArraysAreOfInterest = arraysAreOfInterest;
		}
	}
}
