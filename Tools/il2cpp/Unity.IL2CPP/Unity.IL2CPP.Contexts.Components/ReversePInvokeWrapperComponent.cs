using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Mono.Cecil;
using Unity.Cecil.Awesome.Comparers;
using Unity.Cecil.Awesome.Ordering;
using Unity.IL2CPP.Contexts.Collectors;
using Unity.IL2CPP.Contexts.Components.Base;
using Unity.IL2CPP.Contexts.Forking.Steps;
using Unity.IL2CPP.Contexts.Results.Phases;

namespace Unity.IL2CPP.Contexts.Components
{
	public class ReversePInvokeWrapperComponent : ItemsWithMetadataIndexCollector<MethodReference, IReversePInvokeWrapperCollectorResults, IReversePInvokeWrapperCollector, ReversePInvokeWrapperComponent>, IReversePInvokeWrapperCollector
	{
		private class NotAvailable : IReversePInvokeWrapperCollector
		{
			public void AddReversePInvokeWrapper(MethodReference method)
			{
				throw new NotSupportedException();
			}
		}

		private class Results : MetadataIndexTableResults<MethodReference>, IReversePInvokeWrapperCollectorResults, IMetadataIndexTableResults<MethodReference>, ITableResults<MethodReference, uint>
		{
			public Results(ReadOnlyCollection<MethodReference> sortedItems, ReadOnlyDictionary<MethodReference, uint> table, ReadOnlyCollection<KeyValuePair<MethodReference, uint>> sortedTable)
				: base(sortedItems, table, sortedTable)
			{
			}
		}

		public ReversePInvokeWrapperComponent()
			: base((IEqualityComparer<MethodReference>)new MethodReferenceComparer())
		{
		}

		public void AddReversePInvokeWrapper(MethodReference method)
		{
			AddInternal(method);
		}

		protected override ReversePInvokeWrapperComponent CreateEmptyInstance()
		{
			return new ReversePInvokeWrapperComponent();
		}

		protected override ReversePInvokeWrapperComponent ThisAsFull()
		{
			return this;
		}

		protected override IReversePInvokeWrapperCollector GetNotAvailableWrite()
		{
			return new NotAvailable();
		}

		protected override void ForkForSecondaryCollection(SecondaryCollectionLateAccessForkingContainer lateAccess, out IReversePInvokeWrapperCollector writer, out object reader, out ReversePInvokeWrapperComponent full)
		{
			NotAvailableFork(out writer, out reader, out full);
		}

		protected override void ForkForPrimaryWrite(PrimaryWriteAssembliesLateAccessForkingContainer lateAccess, out IReversePInvokeWrapperCollector writer, out object reader, out ReversePInvokeWrapperComponent full)
		{
			WriteOnlyFork(out writer, out reader, out full);
		}

		protected override void ForkForPrimaryCollection(PrimaryCollectionLateAccessForkingContainer lateAccess, out IReversePInvokeWrapperCollector writer, out object reader, out ReversePInvokeWrapperComponent full)
		{
			NotAvailableFork(out writer, out reader, out full);
		}

		protected override void ForkForSecondaryWrite(SecondaryWriteLateAccessForkingContainer lateAccess, out IReversePInvokeWrapperCollector writer, out object reader, out ReversePInvokeWrapperComponent full)
		{
			NotAvailableFork(out writer, out reader, out full);
		}

		protected override ReadOnlyCollection<MethodReference> SortItems(IEnumerable<MethodReference> items)
		{
			return items.ToSortedCollection();
		}

		protected override IReversePInvokeWrapperCollectorResults CreateResultObject(ReadOnlyCollection<MethodReference> sortedItems, ReadOnlyDictionary<MethodReference, uint> table, ReadOnlyCollection<KeyValuePair<MethodReference, uint>> sortedTable)
		{
			return new Results(sortedItems, table, sortedTable);
		}
	}
}
