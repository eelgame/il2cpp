using System.Collections.Generic;
using Unity.Cecil.Awesome.Comparers;

namespace Unity.IL2CPP
{
	internal class Il2CppRuntimeFieldReferenceEqualityComparer : FieldReferenceComparer, IEqualityComparer<Il2CppRuntimeFieldReference>
	{
		public bool Equals(Il2CppRuntimeFieldReference x, Il2CppRuntimeFieldReference y)
		{
			return Equals(x.Field, y.Field);
		}

		public int GetHashCode(Il2CppRuntimeFieldReference obj)
		{
			return GetHashCode(obj.Field);
		}
	}
}
