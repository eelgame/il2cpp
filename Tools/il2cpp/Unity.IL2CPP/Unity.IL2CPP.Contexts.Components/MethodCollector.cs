using System.Collections.Generic;
using System.Collections.ObjectModel;
using Mono.Cecil;
using Unity.Cecil.Awesome.Comparers;
using Unity.Cecil.Awesome.Ordering;
using Unity.IL2CPP.Contexts.Components.Base;
using Unity.IL2CPP.Contexts.Forking.Steps;
using Unity.IL2CPP.Contexts.Results.Phases;

namespace Unity.IL2CPP.Contexts.Components
{
	public class MethodCollector : ItemsWithMetadataIndexCollector<MethodReference, IMethodCollectorResults, IMethodCollector, MethodCollector>, IMethodCollector
	{
		private class Results : MetadataIndexTableResults<MethodReference>, IMethodCollectorResults, IMetadataIndexTableResults<MethodReference>, ITableResults<MethodReference, uint>
		{
			public Results(ReadOnlyCollection<MethodReference> sortedItems, ReadOnlyDictionary<MethodReference, uint> table, ReadOnlyCollection<KeyValuePair<MethodReference, uint>> sortedTable)
				: base(sortedItems, table, sortedTable)
			{
			}
		}

		private class NotAvailable : ItemsWithMetadataIndexCollectorNotAvailable<MethodReference>, IMethodCollector, IMethodCollectorResults, IMetadataIndexTableResults<MethodReference>, ITableResults<MethodReference, uint>
		{
		}

		public MethodCollector()
			: base((IEqualityComparer<MethodReference>)new MethodReferenceComparer())
		{
		}

		public void Add(MethodReference method)
		{
			AddInternal(method);
		}

		protected override ReadOnlyCollection<MethodReference> SortItems(IEnumerable<MethodReference> items)
		{
			return items.ToSortedCollection();
		}

		protected override IMethodCollectorResults CreateResultObject(ReadOnlyCollection<MethodReference> sortedItems, ReadOnlyDictionary<MethodReference, uint> table, ReadOnlyCollection<KeyValuePair<MethodReference, uint>> sortedTable)
		{
			return new Results(sortedItems, table, sortedTable);
		}

		protected override void ForkForSecondaryCollection(SecondaryCollectionLateAccessForkingContainer lateAccess, out IMethodCollector writer, out object reader, out MethodCollector full)
		{
			NotAvailableFork(out writer, out reader, out full);
		}

		protected override void ForkForPrimaryWrite(PrimaryWriteAssembliesLateAccessForkingContainer lateAccess, out IMethodCollector writer, out object reader, out MethodCollector full)
		{
			WriteOnlyFork(out writer, out reader, out full);
		}

		protected override void ForkForPrimaryCollection(PrimaryCollectionLateAccessForkingContainer lateAccess, out IMethodCollector writer, out object reader, out MethodCollector full)
		{
			NotAvailableFork(out writer, out reader, out full);
		}

		protected override void ForkForSecondaryWrite(SecondaryWriteLateAccessForkingContainer lateAccess, out IMethodCollector writer, out object reader, out MethodCollector full)
		{
			NotAvailableFork(out writer, out reader, out full);
		}

		protected override MethodCollector CreateEmptyInstance()
		{
			return new MethodCollector();
		}

		protected override MethodCollector ThisAsFull()
		{
			return this;
		}

		protected override IMethodCollector GetNotAvailableWrite()
		{
			return new NotAvailable();
		}
	}
}
