using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Unity.IL2CPP.Diagnostics;

namespace Unity.IL2CPP.Contexts.Components.Base
{
	public abstract class ForkAndMergeCollectionCollectorBase<TItem, TComplete, TWrite, TFull> : CompletableComponentBase<TComplete, TWrite, TFull>, IDumpableState where TFull : ForkAndMergeCollectionCollectorBase<TItem, TComplete, TWrite, TFull>, TWrite
	{
		private readonly ICollection<TItem> _items;

		protected ForkAndMergeCollectionCollectorBase(ICollection<TItem> items)
		{
			_items = items;
		}

		protected virtual void AddInternal(TItem item)
		{
			AssertNotComplete();
			_items.Add(item);
		}

		protected bool ContainsInternal(TItem item)
		{
			AssertNotComplete();
			return _items.Contains(item);
		}

		protected override TComplete GetResults()
		{
			return BuildResults(SortItems(_items));
		}

		protected ReadOnlyCollection<TItem> CompleteForMerge()
		{
			SetComplete();
			return _items.ToList().AsReadOnly();
		}

		protected override void HandleMergeForAdd(TFull forked)
		{
			foreach (TItem item in forked.CompleteForMerge())
			{
				AddInternal(item);
			}
		}

		protected override void HandleMergeForMergeValues(TFull forked)
		{
			throw new NotSupportedException();
		}

		void IDumpableState.DumpState(StringBuilder builder)
		{
			CollectorStateDumper.AppendCollection(builder, "_items", SortItems(_items), DumpStateItemToString);
		}

		protected virtual string DumpStateItemToString(TItem item)
		{
			return item.ToString();
		}

		protected abstract ReadOnlyCollection<TItem> SortItems(IEnumerable<TItem> items);

		protected abstract TComplete BuildResults(ReadOnlyCollection<TItem> sortedItem);
	}
}
