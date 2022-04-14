using System.Collections.Generic;
using Mono.Cecil;

namespace Unity.IL2CPP.GenericsCollection.CodeFlow.GraphGenerationSteps
{
	internal static class AddMethodsToDefinitionsOfInterestStep
	{
		internal static void Run(HashSet<IMemberDefinition> definitionsOfInterest)
		{
			IMemberDefinition[] array = new IMemberDefinition[definitionsOfInterest.Count];
			definitionsOfInterest.CopyTo(array);
			IMemberDefinition[] array2 = array;
			for (int i = 0; i < array2.Length; i++)
			{
				if (!(array2[i] is TypeDefinition typeDefinition))
				{
					continue;
				}
				foreach (MethodDefinition method in typeDefinition.Methods)
				{
					definitionsOfInterest.Add(method);
				}
			}
		}
	}
}
