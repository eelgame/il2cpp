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
	public class InteropGuidComponent : ForkAndMergeListCollectorBase<IIl2CppRuntimeType, ReadOnlyCollection<IIl2CppRuntimeType>, IInteropGuidCollector, InteropGuidComponent>, IInteropGuidCollector
	{
		private class NotAvailable : IInteropGuidCollector
		{
			public void Add(SourceWritingContext context, TypeReference type)
			{
				throw new NotSupportedException();
			}
		}

		public void Add(SourceWritingContext context, TypeReference type)
		{
			AddInternal(context.Global.Collectors.Types.Add(type));
		}

		protected override InteropGuidComponent CreateEmptyInstance()
		{
			return new InteropGuidComponent();
		}

		protected override InteropGuidComponent CreateCopyInstance()
		{
			throw new NotSupportedException();
		}

		protected override InteropGuidComponent ThisAsFull()
		{
			return this;
		}

		protected override IInteropGuidCollector GetNotAvailableWrite()
		{
			return new NotAvailable();
		}

		protected override void ForkForSecondaryCollection(SecondaryCollectionLateAccessForkingContainer lateAccess, out IInteropGuidCollector writer, out object reader, out InteropGuidComponent full)
		{
			NotAvailableFork(out writer, out reader, out full);
		}

		protected override void ForkForPrimaryWrite(PrimaryWriteAssembliesLateAccessForkingContainer lateAccess, out IInteropGuidCollector writer, out object reader, out InteropGuidComponent full)
		{
			WriteOnlyFork(out writer, out reader, out full);
		}

		protected override void ForkForPrimaryCollection(PrimaryCollectionLateAccessForkingContainer lateAccess, out IInteropGuidCollector writer, out object reader, out InteropGuidComponent full)
		{
			NotAvailableFork(out writer, out reader, out full);
		}

		protected override void ForkForSecondaryWrite(SecondaryWriteLateAccessForkingContainer lateAccess, out IInteropGuidCollector writer, out object reader, out InteropGuidComponent full)
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
