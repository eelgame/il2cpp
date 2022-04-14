using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Mono.Cecil;
using Unity.Cecil.Awesome.Ordering;
using Unity.IL2CPP.Contexts.Collectors;
using Unity.IL2CPP.Contexts.Components.Base;
using Unity.IL2CPP.Contexts.Forking.Steps;

namespace Unity.IL2CPP.Tiny
{
	public class TinyTypeCollectorComponent : ForkAndMergeHashSetCollectorBase<TypeReference, ReadOnlyCollection<TypeReference>, ITinyTypeCollector, TinyTypeCollectorComponent>, ITinyTypeCollector
	{
		private class NotAvailable : ITinyTypeCollector
		{
			public void Add(TypeReference type)
			{
				throw new NotSupportedException();
			}
		}

		public void Add(TypeReference type)
		{
			AddInternal(type);
		}

		protected override ReadOnlyCollection<TypeReference> SortItems(IEnumerable<TypeReference> items)
		{
			return items.ToSortedCollection();
		}

		protected override ReadOnlyCollection<TypeReference> BuildResults(ReadOnlyCollection<TypeReference> sortedItem)
		{
			return sortedItem;
		}

		protected override TinyTypeCollectorComponent CreateEmptyInstance()
		{
			return new TinyTypeCollectorComponent();
		}

		protected override TinyTypeCollectorComponent CreateCopyInstance()
		{
			throw new NotSupportedException();
		}

		protected override TinyTypeCollectorComponent ThisAsFull()
		{
			return this;
		}

		protected override ITinyTypeCollector GetNotAvailableWrite()
		{
			return new NotAvailable();
		}

		protected override void ForkForSecondaryCollection(SecondaryCollectionLateAccessForkingContainer lateAccess, out ITinyTypeCollector writer, out object reader, out TinyTypeCollectorComponent full)
		{
			NotAvailableFork(out writer, out reader, out full);
		}

		protected override void ForkForPrimaryWrite(PrimaryWriteAssembliesLateAccessForkingContainer lateAccess, out ITinyTypeCollector writer, out object reader, out TinyTypeCollectorComponent full)
		{
			WriteOnlyFork(out writer, out reader, out full);
		}

		protected override void ForkForPrimaryCollection(PrimaryCollectionLateAccessForkingContainer lateAccess, out ITinyTypeCollector writer, out object reader, out TinyTypeCollectorComponent full)
		{
			NotAvailableFork(out writer, out reader, out full);
		}

		protected override void ForkForSecondaryWrite(SecondaryWriteLateAccessForkingContainer lateAccess, out ITinyTypeCollector writer, out object reader, out TinyTypeCollectorComponent full)
		{
			NotAvailableFork(out writer, out reader, out full);
		}
	}
}
