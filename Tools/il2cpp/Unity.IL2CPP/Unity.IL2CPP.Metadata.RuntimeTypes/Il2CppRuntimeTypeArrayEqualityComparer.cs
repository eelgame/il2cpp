using System.Collections.Generic;

namespace Unity.IL2CPP.Metadata.RuntimeTypes
{
	internal class Il2CppRuntimeTypeArrayEqualityComparer : IEqualityComparer<IIl2CppRuntimeType[]>
	{
		private readonly Il2CppRuntimeTypeEqualityComparer _elementComparer = new Il2CppRuntimeTypeEqualityComparer();

		public bool Equals(IIl2CppRuntimeType[] x, IIl2CppRuntimeType[] y)
		{
			if (x.Length != y.Length)
			{
				return false;
			}
			for (int i = 0; i < x.Length; i++)
			{
				if (!_elementComparer.Equals(x[i], y[i]))
				{
					return false;
				}
			}
			return true;
		}

		public int GetHashCode(IIl2CppRuntimeType[] obj)
		{
			int num = 31 * obj.Length;
			for (int i = 0; i < obj.Length; i++)
			{
				num += 7 * _elementComparer.GetHashCode(obj[i]);
			}
			return num;
		}
	}
}
