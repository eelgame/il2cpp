using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Unity.IL2CPP.Contexts.Components.Base
{
	public interface ITableResults<TKey, TValue>
	{
		ReadOnlyCollection<TKey> SortedKeys { get; }

		ReadOnlyCollection<KeyValuePair<TKey, TValue>> SortedItems { get; }

		ReadOnlyCollection<TKey> UnsortedKeys { get; }

		ReadOnlyCollection<KeyValuePair<TKey, TValue>> UnsortedItems { get; }

		ReadOnlyCollection<TValue> UnsortedValues { get; }

		ReadOnlyDictionary<TKey, TValue> Table { get; }

		int Count { get; }

		bool TryGetValue(TKey key, out TValue value);

		TValue GetValue(TKey key);
	}
}
