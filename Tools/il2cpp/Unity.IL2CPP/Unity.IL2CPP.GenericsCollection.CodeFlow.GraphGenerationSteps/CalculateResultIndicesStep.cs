using System.Collections.Generic;

namespace Unity.IL2CPP.GenericsCollection.CodeFlow.GraphGenerationSteps
{
	internal static class CalculateResultIndicesStep
	{
		public static Dictionary<T, int> Run<T>(List<StagingNode<T>> stagingNodes)
		{
			Dictionary<T, int> dictionary = new Dictionary<T, int>();
			int num = 0;
			int count = stagingNodes.Count;
			for (int i = 0; i < count; i++)
			{
				StagingNode<T> stagingNode = stagingNodes[i];
				if (stagingNode.IsNeeded)
				{
					dictionary[stagingNode.Item] = num++;
				}
			}
			return dictionary;
		}
	}
}
