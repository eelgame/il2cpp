using System.Collections.Generic;

namespace Unity.IL2CPP.Metadata.RuntimeTypes
{
	public class Il2CppRuntimeTypeKeyComparer<TKey, TValue> : IComparer<KeyValuePair<TKey, TValue>> where TKey : IIl2CppRuntimeType
	{
		private readonly Il2CppRuntimeTypeComparer _elementComparer = new Il2CppRuntimeTypeComparer();

		public int Compare(KeyValuePair<TKey, TValue> x, KeyValuePair<TKey, TValue> y)
		{
			return _elementComparer.Compare(x.Key, y.Key);
		}
	}
}
