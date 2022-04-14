using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Unity.IL2CPP.Contexts.Collectors;
using Unity.IL2CPP.Contexts.Components.Base;
using Unity.IL2CPP.Contexts.Forking.Steps;

namespace Unity.IL2CPP.Tiny
{
	public class TinyStringCollectorComponent : ForkAndMergeHashSetCollectorBase<string, ReadOnlyCollection<string>, ITinyStringCollector, TinyStringCollectorComponent>, ITinyStringCollector
	{
		private class NotAvailable : ITinyStringCollector
		{
			public void Add(string literal)
			{
				throw new NotSupportedException();
			}
		}

		public void Add(string literal)
		{
			AddInternal(literal);
		}

		protected override TinyStringCollectorComponent CreateEmptyInstance()
		{
			return new TinyStringCollectorComponent();
		}

		protected override TinyStringCollectorComponent CreateCopyInstance()
		{
			throw new NotSupportedException();
		}

		protected override TinyStringCollectorComponent ThisAsFull()
		{
			return this;
		}

		protected override ITinyStringCollector GetNotAvailableWrite()
		{
			return new NotAvailable();
		}

		protected override void ForkForSecondaryCollection(SecondaryCollectionLateAccessForkingContainer lateAccess, out ITinyStringCollector writer, out object reader, out TinyStringCollectorComponent full)
		{
			NotAvailableFork(out writer, out reader, out full);
		}

		protected override void ForkForPrimaryWrite(PrimaryWriteAssembliesLateAccessForkingContainer lateAccess, out ITinyStringCollector writer, out object reader, out TinyStringCollectorComponent full)
		{
			WriteOnlyFork(out writer, out reader, out full);
		}

		protected override void ForkForPrimaryCollection(PrimaryCollectionLateAccessForkingContainer lateAccess, out ITinyStringCollector writer, out object reader, out TinyStringCollectorComponent full)
		{
			NotAvailableFork(out writer, out reader, out full);
		}

		protected override void ForkForSecondaryWrite(SecondaryWriteLateAccessForkingContainer lateAccess, out ITinyStringCollector writer, out object reader, out TinyStringCollectorComponent full)
		{
			NotAvailableFork(out writer, out reader, out full);
		}

		protected override ReadOnlyCollection<string> SortItems(IEnumerable<string> items)
		{
			return items.ToSortedCollection();
		}

		protected override ReadOnlyCollection<string> BuildResults(ReadOnlyCollection<string> sortedItem)
		{
			return sortedItem;
		}
	}
}
