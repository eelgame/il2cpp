using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Unity.IL2CPP.Contexts.Components.Base
{
	public delegate ReadOnlyCollection<KeyValuePair<TKey, TValue>> LazySortTable<TKey, TValue>(ReadOnlyDictionary<TKey, TValue> table);
}
