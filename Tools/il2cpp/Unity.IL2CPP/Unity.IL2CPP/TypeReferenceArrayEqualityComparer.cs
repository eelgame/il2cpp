using System.Collections.Generic;
using Mono.Cecil;
using Unity.Cecil.Awesome.Comparers;

namespace Unity.IL2CPP
{
	internal class TypeReferenceArrayEqualityComparer : EqualityComparer<TypeReference[]>
	{
		public override bool Equals(TypeReference[] x, TypeReference[] y)
		{
			if (x.Length != y.Length)
			{
				return false;
			}
			for (int i = 0; i < x.Length; i++)
			{
				if (!TypeReferenceEqualityComparer.AreEqual(x[i], y[i]))
				{
					return false;
				}
			}
			return true;
		}

		public override int GetHashCode(TypeReference[] obj)
		{
			int num = 31 * obj.Length;
			for (int i = 0; i < obj.Length; i++)
			{
				num += 7 * TypeReferenceEqualityComparer.GetHashCodeFor(obj[i]);
			}
			return num;
		}
	}
}
