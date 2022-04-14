using System.Collections.Generic;
using Mono.Cecil;
using Unity.Cecil.Awesome.Ordering;
using Unity.IL2CPP.Contexts.Collectors;

namespace Unity.IL2CPP
{
	public class CppIncludeDepthComparer : IComparer<TypeReference>
	{
		private readonly ICppIncludeDepthCalculatorCache _getDependencyDepth;

		public CppIncludeDepthComparer(ICppIncludeDepthCalculatorCache getDependencyDepth)
		{
			_getDependencyDepth = getDependencyDepth;
		}

		public int Compare(TypeReference x, TypeReference y)
		{
			int orCalculateDepth = _getDependencyDepth.GetOrCalculateDepth(x);
			int orCalculateDepth2 = _getDependencyDepth.GetOrCalculateDepth(y);
			if (orCalculateDepth > orCalculateDepth2)
			{
				return 1;
			}
			if (orCalculateDepth < orCalculateDepth2)
			{
				return -1;
			}
			return x.Compare(y);
		}
	}
}
