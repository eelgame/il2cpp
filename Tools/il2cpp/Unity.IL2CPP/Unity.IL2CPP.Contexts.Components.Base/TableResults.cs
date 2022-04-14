using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Unity.IL2CPP.Common;

namespace Unity.IL2CPP.Contexts.Components.Base
{
	public abstract class TableResults<TKey, TValue> : ITableResults<TKey, TValue>
	{
		private readonly ReadOnlyDictionary<TKey, TValue> _table;

		private readonly ReadOnlyCollection<TKey> _sortedKeys;

		private readonly ReadOnlyCollection<KeyValuePair<TKey, TValue>> _sortedItems;

		public ReadOnlyCollection<TKey> SortedKeys => _sortedKeys;

		public ReadOnlyCollection<KeyValuePair<TKey, TValue>> SortedItems => _sortedItems;

		public ReadOnlyCollection<TKey> UnsortedKeys => _sortedKeys;

		public ReadOnlyCollection<KeyValuePair<TKey, TValue>> UnsortedItems => _sortedItems;

		public ReadOnlyCollection<TValue> UnsortedValues => _table.Values.ToArray().AsReadOnly();

		public ReadOnlyDictionary<TKey, TValue> Table => _table;

		public int Count => _table.Count;

		protected TableResults(ReadOnlyDictionary<TKey, TValue> table, ReadOnlyCollection<KeyValuePair<TKey, TValue>> sortedItems, ReadOnlyCollection<TKey> sortedKeys)
		{
			_table = table;
			_sortedItems = sortedItems;
			_sortedKeys = sortedKeys;
		}

		public bool TryGetValue(TKey key, out TValue value)
		{
			return _table.TryGetValue(key, out value);
		}

		public TValue GetValue(TKey key)
		{
			return _table[key];
		}
	}
}
