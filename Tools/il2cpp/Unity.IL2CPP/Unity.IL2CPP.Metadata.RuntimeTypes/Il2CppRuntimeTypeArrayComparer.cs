using System;
using System.Collections.Generic;

namespace Unity.IL2CPP.Metadata.RuntimeTypes
{
	internal class Il2CppRuntimeTypeArrayComparer : IComparer<IIl2CppRuntimeType[]>
	{
		private readonly Il2CppRuntimeTypeComparer m_ElementDataComparer = new Il2CppRuntimeTypeComparer();

		public int Compare(IIl2CppRuntimeType[] x, IIl2CppRuntimeType[] y)
		{
			int num = Math.Min(x.Length, y.Length);
			for (int i = 0; i < num; i++)
			{
				int num2 = m_ElementDataComparer.Compare(x[i], y[i]);
				if (num2 != 0)
				{
					return num2;
				}
			}
			return x.Length - y.Length;
		}
	}
}
