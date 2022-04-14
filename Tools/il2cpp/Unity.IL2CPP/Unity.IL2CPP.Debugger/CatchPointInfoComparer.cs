using System.Collections.Generic;
using Unity.Cecil.Awesome.Comparers;
using Unity.IL2CPP.Common;

namespace Unity.IL2CPP.Debugger
{
	public class CatchPointInfoComparer : IEqualityComparer<CatchPointInfo>
	{
		public bool Equals(CatchPointInfo x, CatchPointInfo y)
		{
			if (x == null && y == null)
			{
				return true;
			}
			if (x == null || y == null)
			{
				return false;
			}
			if (MethodReferenceComparer.AreEqual(x.Method, y.Method))
			{
				return x.IlOffset == y.IlOffset;
			}
			return false;
		}

		public int GetHashCode(CatchPointInfo obj)
		{
			return HashCodeHelper.Combine(MethodReferenceComparer.GetHashCodeFor(obj.Method), obj.IlOffset);
		}
	}
}
