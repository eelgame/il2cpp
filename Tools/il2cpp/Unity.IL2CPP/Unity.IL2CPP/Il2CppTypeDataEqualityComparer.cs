using System.Collections.Generic;
using Unity.Cecil.Awesome.Comparers;

namespace Unity.IL2CPP
{
	internal class Il2CppTypeDataEqualityComparer : EqualityComparer<Il2CppTypeData>
	{
		public override bool Equals(Il2CppTypeData x, Il2CppTypeData y)
		{
			if (x.Attrs == y.Attrs)
			{
				return TypeReferenceEqualityComparer.AreEqual(x.Type, y.Type);
			}
			return false;
		}

		public override int GetHashCode(Il2CppTypeData obj)
		{
			return TypeReferenceEqualityComparer.GetHashCodeFor(obj.Type) + obj.Attrs;
		}
	}
}
