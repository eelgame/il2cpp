using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Unity.IL2CPP.Contexts.Components.Base
{
	public class ItemsWithMetadataIndexCollectorNotAvailable<TItem> : IMetadataIndexTableResults<TItem>, ITableResults<TItem, uint>
	{
		public ReadOnlyCollection<TItem> SortedKeys
		{
			get
			{
				throw new NotSupportedException();
			}
		}

		public ReadOnlyCollection<KeyValuePair<TItem, uint>> SortedItems
		{
			get
			{
				throw new NotSupportedException();
			}
		}

		public ReadOnlyCollection<TItem> UnsortedKeys
		{
			get
			{
				throw new NotSupportedException();
			}
		}

		public ReadOnlyCollection<KeyValuePair<TItem, uint>> UnsortedItems
		{
			get
			{
				throw new NotSupportedException();
			}
		}

		public ReadOnlyCollection<uint> UnsortedValues
		{
			get
			{
				throw new NotSupportedException();
			}
		}

		public ReadOnlyDictionary<TItem, uint> Table
		{
			get
			{
				throw new NotSupportedException();
			}
		}

		public int Count
		{
			get
			{
				throw new NotSupportedException();
			}
		}

		public void Add(TItem item)
		{
			throw new NotSupportedException();
		}

		public bool TryGetValue(TItem key, out uint value)
		{
			throw new NotSupportedException();
		}

		public uint GetValue(TItem key)
		{
			throw new NotSupportedException();
		}

		public uint GetIndex(TItem key)
		{
			throw new NotSupportedException();
		}

		public bool HasIndex(TItem key)
		{
			throw new NotSupportedException();
		}
	}
}
