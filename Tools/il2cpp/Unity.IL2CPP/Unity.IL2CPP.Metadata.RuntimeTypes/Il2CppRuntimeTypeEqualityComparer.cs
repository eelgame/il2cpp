using System.Collections.Generic;
using Unity.Cecil.Awesome.Comparers;
using Unity.IL2CPP.Common;

namespace Unity.IL2CPP.Metadata.RuntimeTypes
{
	public class Il2CppRuntimeTypeEqualityComparer : IEqualityComparer<IIl2CppRuntimeType>
	{
		public bool Equals(IIl2CppRuntimeType x, IIl2CppRuntimeType y)
		{
			if (x.Attrs == y.Attrs)
			{
				return TypeReferenceEqualityComparer.AreEqual(x.Type, y.Type);
			}
			return false;
		}

		public int GetHashCode(IIl2CppRuntimeType obj)
		{
			return HashCodeHelper.Combine(TypeReferenceEqualityComparer.GetHashCodeFor(obj.Type), obj.Attrs);
		}
	}
}
