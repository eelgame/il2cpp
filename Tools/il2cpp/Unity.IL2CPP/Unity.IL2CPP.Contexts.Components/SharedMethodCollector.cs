using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using Mono.Cecil;
using Unity.Cecil.Awesome.Comparers;
using Unity.Cecil.Awesome.Ordering;
using Unity.IL2CPP.Contexts.Collectors;
using Unity.IL2CPP.Contexts.Components.Base;
using Unity.IL2CPP.Contexts.Forking.Steps;

namespace Unity.IL2CPP.Contexts.Components
{
	public class SharedMethodCollector : ForkAndMergeTableCollectorBase<MethodReference, List<MethodReference>, ReadOnlyCollection<MethodReference>, SharedMethodCollection, ISharedMethodCollector, object, SharedMethodCollector>, ISharedMethodCollector
	{
		private class NotAvailable : ISharedMethodCollector
		{
			public void AddSharedMethod(MethodReference sharedMethod, MethodReference actualMethod)
			{
				throw new NotSupportedException();
			}
		}

		public SharedMethodCollector()
			: base((IEqualityComparer<MethodReference>)new MethodReferenceComparer())
		{
		}

		public void AddSharedMethod(MethodReference sharedMethod, MethodReference actualMethod)
		{
			if (!TryGetValueInternal(sharedMethod, out var value))
			{
				value = new List<MethodReference>();
				AddInternal(sharedMethod, value);
			}
			value.Add(actualMethod);
		}

		protected override ReadOnlyCollection<MethodReference> ValueToResultValue(List<MethodReference> value)
		{
			return value.AsReadOnly();
		}

		protected override ReadOnlyCollection<KeyValuePair<MethodReference, TSortValue>> SortTable<TSortValue>(ReadOnlyDictionary<MethodReference, TSortValue> table)
		{
			return table.ItemsSortedByKey();
		}

		protected override ReadOnlyCollection<MethodReference> SortKeys(ICollection<MethodReference> items)
		{
			return items.ToSortedCollection();
		}

		protected override string DumpStateValueToString(List<MethodReference> value)
		{
			StringBuilder stringBuilder = new StringBuilder();
			if (value == null)
			{
				stringBuilder.AppendLine("  null");
			}
			else if (value.Count == 0)
			{
				stringBuilder.AppendLine("  Empty");
			}
			else
			{
				stringBuilder.AppendLine($"    Count = {value.Count}");
				foreach (MethodReference item in value)
				{
					stringBuilder.AppendLine("      " + item.FullName);
				}
			}
			return stringBuilder.ToString();
		}

		protected override SharedMethodCollection CreateResultObject(ReadOnlyDictionary<MethodReference, ReadOnlyCollection<MethodReference>> table, ReadOnlyCollection<KeyValuePair<MethodReference, ReadOnlyCollection<MethodReference>>> sortedItems, ReadOnlyCollection<MethodReference> sortedKeys)
		{
			return new SharedMethodCollection(table, sortedItems, sortedKeys);
		}

		protected override SharedMethodCollector CreateEmptyInstance()
		{
			return new SharedMethodCollector();
		}

		protected override SharedMethodCollector CreateCopyInstance()
		{
			throw new NotSupportedException();
		}

		protected override SharedMethodCollector ThisAsFull()
		{
			return this;
		}

		protected override object ThisAsRead()
		{
			throw new NotSupportedException();
		}

		protected override ISharedMethodCollector GetNotAvailableWrite()
		{
			return new NotAvailable();
		}

		protected override object GetNotAvailableRead()
		{
			throw new NotSupportedException();
		}

		protected override void ForkForSecondaryCollection(SecondaryCollectionLateAccessForkingContainer lateAccess, out ISharedMethodCollector writer, out object reader, out SharedMethodCollector full)
		{
			NotAvailableFork(out writer, out reader, out full);
		}

		protected override void ForkForPrimaryWrite(PrimaryWriteAssembliesLateAccessForkingContainer lateAccess, out ISharedMethodCollector writer, out object reader, out SharedMethodCollector full)
		{
			((ComponentBase<ISharedMethodCollector, object, SharedMethodCollector>)this).WriteOnlyFork(out writer, out reader, out full, ForkMode.Empty, MergeMode.MergeValues);
		}

		protected override void ForkForPrimaryCollection(PrimaryCollectionLateAccessForkingContainer lateAccess, out ISharedMethodCollector writer, out object reader, out SharedMethodCollector full)
		{
			NotAvailableFork(out writer, out reader, out full);
		}

		protected override void ForkForSecondaryWrite(SecondaryWriteLateAccessForkingContainer lateAccess, out ISharedMethodCollector writer, out object reader, out SharedMethodCollector full)
		{
			NotAvailableFork(out writer, out reader, out full);
		}

		protected override bool DoValuesConflictForAddMergeMode(List<MethodReference> thisValue, List<MethodReference> otherValue)
		{
			throw new NotSupportedException();
		}

		protected override List<MethodReference> MergeValuesForMergeMergeMode(List<MethodReference> thisValue, List<MethodReference> otherValue)
		{
			thisValue.AddRange(otherValue);
			return thisValue;
		}
	}
}
