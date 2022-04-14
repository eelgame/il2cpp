using System.Collections.Generic;
using Mono.Cecil;
using Unity.Cecil.Awesome.Comparers;

namespace Unity.IL2CPP.Metadata
{
	internal class Il2CppGenericInstComparer : EqualityComparer<IList<TypeReference>>
	{
		public override bool Equals(IList<TypeReference> x, IList<TypeReference> y)
		{
			if (x.Count != y.Count)
			{
				return false;
			}
			for (int i = 0; i < x.Count; i++)
			{
				if (!TypeReferenceEqualityComparer.AreEqual(x[i], y[i]))
				{
					return false;
				}
			}
			return true;
		}

		public override int GetHashCode(IList<TypeReference> obj)
		{
			int num = obj.Count;
			for (int i = 0; i < obj.Count; i++)
			{
				num = num * 486187739 + TypeReferenceEqualityComparer.GetHashCodeFor(obj[i]);
			}
			return num;
		}
	}
}
