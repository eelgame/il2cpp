using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Mono.Cecil;
using Unity.IL2CPP.Contexts.Collectors;
using Unity.IL2CPP.Contexts.Components.Base;
using Unity.IL2CPP.Contexts.Forking.Steps;
using Unity.IL2CPP.Metadata.RuntimeTypes;

namespace Unity.IL2CPP.Contexts.Components
{
	public class CCWMarshallingFunctionComponent : ForkAndMergeListCollectorBase<IIl2CppRuntimeType, ReadOnlyCollection<IIl2CppRuntimeType>, ICCWMarshallingFunctionCollector, CCWMarshallingFunctionComponent>, ICCWMarshallingFunctionCollector
	{
		private class NotAvailable : ICCWMarshallingFunctionCollector
		{
			public void Add(PrimaryCollectionContext context, TypeReference type)
			{
				throw new NotSupportedException();
			}
		}

		public void Add(PrimaryCollectionContext context, TypeReference type)
		{
			AddInternal(context.Global.Collectors.Types.Add(type));
		}

		protected override CCWMarshallingFunctionComponent CreateEmptyInstance()
		{
			return new CCWMarshallingFunctionComponent();
		}

		protected override CCWMarshallingFunctionComponent CreateCopyInstance()
		{
			throw new NotSupportedException();
		}

		protected override CCWMarshallingFunctionComponent ThisAsFull()
		{
			return this;
		}

		protected override ICCWMarshallingFunctionCollector GetNotAvailableWrite()
		{
			return new NotAvailable();
		}

		protected override void ForkForSecondaryCollection(SecondaryCollectionLateAccessForkingContainer lateAccess, out ICCWMarshallingFunctionCollector writer, out object reader, out CCWMarshallingFunctionComponent full)
		{
			NotAvailableFork(out writer, out reader, out full);
		}

		protected override void ForkForPrimaryWrite(PrimaryWriteAssembliesLateAccessForkingContainer lateAccess, out ICCWMarshallingFunctionCollector writer, out object reader, out CCWMarshallingFunctionComponent full)
		{
			NotAvailableFork(out writer, out reader, out full);
		}

		protected override void ForkForPrimaryCollection(PrimaryCollectionLateAccessForkingContainer lateAccess, out ICCWMarshallingFunctionCollector writer, out object reader, out CCWMarshallingFunctionComponent full)
		{
			WriteOnlyFork(out writer, out reader, out full);
		}

		protected override void ForkForSecondaryWrite(SecondaryWriteLateAccessForkingContainer lateAccess, out ICCWMarshallingFunctionCollector writer, out object reader, out CCWMarshallingFunctionComponent full)
		{
			NotAvailableFork(out writer, out reader, out full);
		}

		protected override ReadOnlyCollection<IIl2CppRuntimeType> SortItems(IEnumerable<IIl2CppRuntimeType> items)
		{
			return items.ToSortedCollection();
		}

		protected override ReadOnlyCollection<IIl2CppRuntimeType> BuildResults(ReadOnlyCollection<IIl2CppRuntimeType> sortedItem)
		{
			return sortedItem;
		}
	}
}
