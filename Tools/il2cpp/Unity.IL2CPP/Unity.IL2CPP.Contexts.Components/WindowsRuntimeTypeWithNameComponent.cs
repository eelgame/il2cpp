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
	public class WindowsRuntimeTypeWithNameComponent : ForkAndMergeListCollectorBase<Tuple<IIl2CppRuntimeType, string>, ReadOnlyCollection<Tuple<IIl2CppRuntimeType, string>>, IWindowsRuntimeTypeWithNameCollector, WindowsRuntimeTypeWithNameComponent>, IWindowsRuntimeTypeWithNameCollector
	{
		private class NotAvailable : IWindowsRuntimeTypeWithNameCollector
		{
			public void AddWindowsRuntimeTypeWithName(PrimaryCollectionContext context, TypeReference type, string typeName)
			{
				throw new NotSupportedException();
			}
		}

		private class Comparer : IComparer<Tuple<IIl2CppRuntimeType, string>>
		{
			private Il2CppRuntimeTypeComparer _typeComparer = new Il2CppRuntimeTypeComparer();

			public int Compare(Tuple<IIl2CppRuntimeType, string> x, Tuple<IIl2CppRuntimeType, string> y)
			{
				return _typeComparer.Compare(x.Item1, y.Item1);
			}
		}

		public void AddWindowsRuntimeTypeWithName(PrimaryCollectionContext context, TypeReference type, string typeName)
		{
			AddInternal(new Tuple<IIl2CppRuntimeType, string>(context.Global.Collectors.Types.Add(type), typeName));
			context.Global.Collectors.Stats.RecordWindowsRuntimeTypeWithName();
		}

		protected override WindowsRuntimeTypeWithNameComponent CreateEmptyInstance()
		{
			return new WindowsRuntimeTypeWithNameComponent();
		}

		protected override WindowsRuntimeTypeWithNameComponent CreateCopyInstance()
		{
			throw new NotSupportedException();
		}

		protected override WindowsRuntimeTypeWithNameComponent ThisAsFull()
		{
			return this;
		}

		protected override IWindowsRuntimeTypeWithNameCollector GetNotAvailableWrite()
		{
			return new NotAvailable();
		}

		protected override string DumpStateItemToString(Tuple<IIl2CppRuntimeType, string> item)
		{
			return $"{item.Item1}, {item.Item2}";
		}

		protected override void ForkForSecondaryCollection(SecondaryCollectionLateAccessForkingContainer lateAccess, out IWindowsRuntimeTypeWithNameCollector writer, out object reader, out WindowsRuntimeTypeWithNameComponent full)
		{
			NotAvailableFork(out writer, out reader, out full);
		}

		protected override void ForkForPrimaryWrite(PrimaryWriteAssembliesLateAccessForkingContainer lateAccess, out IWindowsRuntimeTypeWithNameCollector writer, out object reader, out WindowsRuntimeTypeWithNameComponent full)
		{
			NotAvailableFork(out writer, out reader, out full);
		}

		protected override void ForkForPrimaryCollection(PrimaryCollectionLateAccessForkingContainer lateAccess, out IWindowsRuntimeTypeWithNameCollector writer, out object reader, out WindowsRuntimeTypeWithNameComponent full)
		{
			WriteOnlyFork(out writer, out reader, out full);
		}

		protected override void ForkForSecondaryWrite(SecondaryWriteLateAccessForkingContainer lateAccess, out IWindowsRuntimeTypeWithNameCollector writer, out object reader, out WindowsRuntimeTypeWithNameComponent full)
		{
			NotAvailableFork(out writer, out reader, out full);
		}

		protected override ReadOnlyCollection<Tuple<IIl2CppRuntimeType, string>> SortItems(IEnumerable<Tuple<IIl2CppRuntimeType, string>> items)
		{
			return items.ToSortedCollection(new Comparer());
		}

		protected override ReadOnlyCollection<Tuple<IIl2CppRuntimeType, string>> BuildResults(ReadOnlyCollection<Tuple<IIl2CppRuntimeType, string>> sortedItem)
		{
			return sortedItem;
		}
	}
}
