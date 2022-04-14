using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Unity.IL2CPP.Common;

namespace Unity.IL2CPP.Contexts.Components.Base
{
	public abstract class MetadataIndexTableResults<TItem> : IMetadataIndexTableResults<TItem>, ITableResults<TItem, uint>
	{
		private readonly ReadOnlyCollection<TItem> _sortedItems;

		private readonly ReadOnlyDictionary<TItem, uint> _table;

		private readonly ReadOnlyCollection<KeyValuePair<TItem, uint>> _sortedTable;

		public ReadOnlyCollection<TItem> SortedKeys => _sortedItems;

		public ReadOnlyCollection<KeyValuePair<TItem, uint>> SortedItems => _sortedTable;

		public ReadOnlyCollection<TItem> UnsortedKeys => _sortedItems;

		public ReadOnlyCollection<KeyValuePair<TItem, uint>> UnsortedItems => _sortedTable;

		public ReadOnlyCollection<uint> UnsortedValues => _table.Values.ToArray().AsReadOnly();

		public ReadOnlyDictionary<TItem, uint> Table => _table;

		public int Count => _sortedItems.Count;

		public MetadataIndexTableResults(ReadOnlyCollection<TItem> sortedItems, ReadOnlyDictionary<TItem, uint> table, ReadOnlyCollection<KeyValuePair<TItem, uint>> sortedTable)
		{
			_sortedItems = sortedItems;
			_table = table;
			_sortedTable = sortedTable;
		}

		public bool TryGetValue(TItem key, out uint value)
		{
			return _table.TryGetValue(key, out value);
		}

		public uint GetValue(TItem key)
		{
			return _table[key];
		}

		public virtual uint GetIndex(TItem key)
		{
			return GetValue(key);
		}

		public bool HasIndex(TItem key)
		{
			return _table.ContainsKey(key);
		}
	}
}
