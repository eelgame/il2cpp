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
	public class TypeMarshallingFunctionsComponent : ForkAndMergeListCollectorBase<IIl2CppRuntimeType, ReadOnlyCollection<IIl2CppRuntimeType>, ITypeMarshallingFunctionsCollector, TypeMarshallingFunctionsComponent>, ITypeMarshallingFunctionsCollector
	{
		private class NotAvailable : ITypeMarshallingFunctionsCollector
		{
			public void Add(SourceWritingContext context, TypeDefinition type)
			{
				throw new NotSupportedException();
			}

			public void Add(ITypeCollector typeCollector, TypeDefinition type)
			{
				throw new NotSupportedException();
			}
		}

		public void Add(SourceWritingContext context, TypeDefinition type)
		{
			AddInternal(context.Global.Collectors.Types.Add(type));
		}

		protected override TypeMarshallingFunctionsComponent CreateEmptyInstance()
		{
			return new TypeMarshallingFunctionsComponent();
		}

		protected override TypeMarshallingFunctionsComponent CreateCopyInstance()
		{
			throw new NotSupportedException();
		}

		protected override TypeMarshallingFunctionsComponent ThisAsFull()
		{
			return this;
		}

		protected override ITypeMarshallingFunctionsCollector GetNotAvailableWrite()
		{
			return new NotAvailable();
		}

		protected override void ForkForSecondaryCollection(SecondaryCollectionLateAccessForkingContainer lateAccess, out ITypeMarshallingFunctionsCollector writer, out object reader, out TypeMarshallingFunctionsComponent full)
		{
			NotAvailableFork(out writer, out reader, out full);
		}

		protected override void ForkForPrimaryWrite(PrimaryWriteAssembliesLateAccessForkingContainer lateAccess, out ITypeMarshallingFunctionsCollector writer, out object reader, out TypeMarshallingFunctionsComponent full)
		{
			WriteOnlyFork(out writer, out reader, out full);
		}

		protected override void ForkForPrimaryCollection(PrimaryCollectionLateAccessForkingContainer lateAccess, out ITypeMarshallingFunctionsCollector writer, out object reader, out TypeMarshallingFunctionsComponent full)
		{
			NotAvailableFork(out writer, out reader, out full);
		}

		protected override void ForkForSecondaryWrite(SecondaryWriteLateAccessForkingContainer lateAccess, out ITypeMarshallingFunctionsCollector writer, out object reader, out TypeMarshallingFunctionsComponent full)
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
