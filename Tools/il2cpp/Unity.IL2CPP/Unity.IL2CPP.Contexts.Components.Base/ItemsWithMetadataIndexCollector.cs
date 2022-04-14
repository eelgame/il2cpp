using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Unity.IL2CPP.Common;

namespace Unity.IL2CPP.Contexts.Components.Base
{
	public abstract class ItemsWithMetadataIndexCollector<TItem, TComplete, TWrite, TFull> : ForkAndMergeHashSetCollectorBase<TItem, TComplete, TWrite, TFull> where TComplete : IMetadataIndexTableResults<TItem> where TFull : ForkAndMergeHashSetCollectorBase<TItem, TComplete, TWrite, TFull>, TWrite
	{
		private readonly IEqualityComparer<TItem> _comparer;

		public ItemsWithMetadataIndexCollector(IEqualityComparer<TItem> comparer)
			: base(comparer)
		{
			_comparer = comparer;
		}

		protected override TComplete BuildResults(ReadOnlyCollection<TItem> sortedItem)
		{
			Dictionary<TItem, uint> dictionary = ((_comparer == null) ? new Dictionary<TItem, uint>() : new Dictionary<TItem, uint>(_comparer));
			List<KeyValuePair<TItem, uint>> list = new List<KeyValuePair<TItem, uint>>();
			uint num = 0u;
			foreach (TItem item in sortedItem)
			{
				dictionary.Add(item, num);
				list.Add(new KeyValuePair<TItem, uint>(item, num));
				num++;
			}
			return CreateResultObject(sortedItem, dictionary.AsReadOnly(), list.AsReadOnly());
		}

		protected override TFull CreateCopyInstance()
		{
			throw new NotSupportedException("Normally index collectors do not use copy");
		}

		protected abstract TComplete CreateResultObject(ReadOnlyCollection<TItem> sortedItems, ReadOnlyDictionary<TItem, uint> table, ReadOnlyCollection<KeyValuePair<TItem, uint>> sortedTable);
	}
}
