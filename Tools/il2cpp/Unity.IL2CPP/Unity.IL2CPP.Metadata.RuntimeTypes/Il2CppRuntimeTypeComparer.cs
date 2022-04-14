using System.Collections.Generic;
using Unity.Cecil.Awesome.Ordering;

namespace Unity.IL2CPP.Metadata.RuntimeTypes
{
	public class Il2CppRuntimeTypeComparer : IComparer<IIl2CppRuntimeType>
	{
		public int Compare(IIl2CppRuntimeType x, IIl2CppRuntimeType y)
		{
			int num = x.Type.Compare(y.Type);
			if (num != 0)
			{
				return num;
			}
			num = x.Attrs - y.Attrs;
			if (num != 0)
			{
				return num;
			}
			return 0;
		}
	}
}
