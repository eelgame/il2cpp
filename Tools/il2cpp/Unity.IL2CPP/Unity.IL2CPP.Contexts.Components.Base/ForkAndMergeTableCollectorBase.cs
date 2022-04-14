using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Diagnostics;

namespace Unity.IL2CPP.Contexts.Components.Base
{
	public abstract class ForkAndMergeTableCollectorBase<TKey, TValue, TResultValue, TComplete, TWrite, TRead, TFull> : ComponentBase<TWrite, TRead, TFull>, IDumpableState where TComplete : ITableResults<TKey, TResultValue> where TFull : ForkAndMergeTableCollectorBase<TKey, TValue, TResultValue, TComplete, TWrite, TRead, TFull>, TWrite, TRead
	{
		private readonly IEqualityComparer<TKey> _comparer;

		private readonly Dictionary<TKey, TValue> _items;

		private bool _complete;

		protected ReadOnlyDictionary<TKey, TValue> ItemsInternal => _items.AsReadOnly();

		protected int CountInternal => _items.Count;

		protected ForkAndMergeTableCollectorBase(IEqualityComparer<TKey> comparer)
		{
			_comparer = comparer;
			_items = ((comparer == null) ? new Dictionary<TKey, TValue>() : new Dictionary<TKey, TValue>(comparer));
		}

		protected ForkAndMergeTableCollectorBase(IEqualityComparer<TKey> comparer, ReadOnlyDictionary<TKey, TValue> existingData)
		{
			_comparer = comparer;
			_items = ((comparer == null) ? new Dictionary<TKey, TValue>(existingData) : new Dictionary<TKey, TValue>(existingData, comparer));
		}

		protected TValue GetValueInternal(TKey key)
		{
			return _items[key];
		}

		protected virtual void AddInternal(TKey key, TValue value)
		{
			AssertNotComplete();
			_items.Add(key, value);
		}

		protected bool ContainsInternal(TKey key)
		{
			AssertNotComplete();
			return _items.ContainsKey(key);
		}

		protected bool TryGetValueInternal(TKey key, out TValue value)
		{
			AssertNotComplete();
			return _items.TryGetValue(key, out value);
		}

		public virtual TComplete Complete()
		{
			AssertNotComplete();
			_complete = true;
			return BuildResults(_items.AsReadOnly());
		}

		protected ReadOnlyDictionary<TKey, TValue> CompleteForMerge()
		{
			_complete = true;
			return _items.AsReadOnly();
		}

		protected override void HandleMergeForAdd(TFull forked)
		{
			foreach (KeyValuePair<TKey, TValue> item in forked.CompleteForMerge())
			{
				if (_items.TryGetValue(item.Key, out var value))
				{
					if (DoValuesConflictForAddMergeMode(item.Value, value))
					{
						throw new InvalidOperationException($"Conflict for `{item.Key}`.  Parent has `{value}` while forked had {item.Value}");
					}
				}
				else
				{
					AddInternal(item.Key, item.Value);
				}
			}
		}

		protected override void HandleMergeForMergeValues(TFull forked)
		{
			foreach (KeyValuePair<TKey, TValue> item in forked.CompleteForMerge())
			{
				if (_items.TryGetValue(item.Key, out var value))
				{
					MergeValuesForMergeMergeMode(value, item.Value);
				}
				else
				{
					AddInternal(item.Key, item.Value);
				}
			}
		}

		protected abstract bool DoValuesConflictForAddMergeMode(TValue thisValue, TValue otherValue);

		protected abstract TValue MergeValuesForMergeMergeMode(TValue thisValue, TValue otherValue);

		protected void AssertComplete()
		{
			if (!_complete)
			{
				throw new InvalidOperationException("This method cannot be used until Complete() has been called.");
			}
		}

		protected void AssertNotComplete()
		{
			if (_complete)
			{
				throw new InvalidOperationException("Once Complete() has been called, items cannot be added");
			}
		}

		void IDumpableState.DumpState(StringBuilder builder)
		{
			CollectorStateDumper.AppendTable(builder, "_items", SortTable(_items.AsReadOnly()), DumpStateKeyToString, DumpStateValueToString);
		}

		protected virtual string DumpStateKeyToString(TKey key)
		{
			if (key is ICollection)
			{
				throw new NotSupportedException("ToString on an collection will not format well.  You should override this method and provider better formatting");
			}
			return key.ToString();
		}

		protected virtual string DumpStateValueToString(TValue value)
		{
			if (value is ICollection)
			{
				throw new NotSupportedException("ToString on an collection will not format well.  You should override this method and provider better formatting");
			}
			return value.ToString();
		}

		protected virtual TComplete BuildResults(ReadOnlyDictionary<TKey, TValue> table)
		{
			Dictionary<TKey, TResultValue> dictionary = ((_comparer == null) ? new Dictionary<TKey, TResultValue>() : new Dictionary<TKey, TResultValue>(_comparer));
			ReadOnlyCollection<TKey> readOnlyCollection = SortKeys(table.Keys);
			List<KeyValuePair<TKey, TResultValue>> list = new List<KeyValuePair<TKey, TResultValue>>();
			foreach (TKey item in readOnlyCollection)
			{
				TValue value = table[item];
				TResultValue value2 = ValueToResultValue(value);
				dictionary.Add(item, value2);
				list.Add(new KeyValuePair<TKey, TResultValue>(item, value2));
			}
			return CreateResultObject(dictionary.AsReadOnly(), list.AsReadOnly(), readOnlyCollection);
		}

		protected abstract TResultValue ValueToResultValue(TValue value);

		protected abstract ReadOnlyCollection<KeyValuePair<TKey, TSortValue>> SortTable<TSortValue>(ReadOnlyDictionary<TKey, TSortValue> table);

		protected abstract ReadOnlyCollection<TKey> SortKeys(ICollection<TKey> items);

		protected abstract TComplete CreateResultObject(ReadOnlyDictionary<TKey, TResultValue> table, ReadOnlyCollection<KeyValuePair<TKey, TResultValue>> sortedItems, ReadOnlyCollection<TKey> sortedKeys);
	}
}
