using System.Collections.Generic;
using Unity.Cecil.Awesome.Comparers;

namespace Unity.IL2CPP.Metadata
{
	public class Il2CppMethodSpecEqualityComparer : EqualityComparer<Il2CppMethodSpec>
	{
		public override bool Equals(Il2CppMethodSpec x, Il2CppMethodSpec y)
		{
			return MethodReferenceComparer.AreEqual(x.GenericMethod, y.GenericMethod);
		}

		public override int GetHashCode(Il2CppMethodSpec obj)
		{
			return MethodReferenceComparer.GetHashCodeFor(obj.GenericMethod);
		}
	}
}
