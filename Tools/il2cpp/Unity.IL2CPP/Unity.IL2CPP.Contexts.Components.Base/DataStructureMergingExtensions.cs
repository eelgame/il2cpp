using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Unity.IL2CPP.Common;

namespace Unity.IL2CPP.Contexts.Components.Base
{
	public static class DataStructureMergingExtensions
	{
		public static void Merge<T>(this HashSet<T> parent, HashSet<T> forked)
		{
			parent.UnionWith(forked);
		}

		public static void MergeNoConflictsAllowed<TKey, TValue>(this Dictionary<TKey, TValue> parent, Dictionary<TKey, TValue> forked, Func<TValue, TValue, bool> valuesAreEqual)
		{
			foreach (KeyValuePair<TKey, TValue> item in forked)
			{
				if (parent.TryGetValue(item.Key, out var value))
				{
					if (!valuesAreEqual(value, item.Value))
					{
						throw new InvalidOperationException($"Conflict for `{item.Key}`.  Parent has `{value}` while forked had {item.Value}");
					}
				}
				else
				{
					parent[item.Key] = item.Value;
				}
			}
		}

		public static void MergeWithMergeConflicts<TKey, TValue>(this Dictionary<TKey, TValue> parent, Dictionary<TKey, TValue> forked, Func<TValue, TValue, TValue> mergeValues)
		{
			foreach (KeyValuePair<TKey, TValue> item in forked)
			{
				if (parent.TryGetValue(item.Key, out var value))
				{
					parent[item.Key] = mergeValues(value, item.Value);
				}
				else
				{
					parent[item.Key] = item.Value;
				}
			}
		}

		public static ReadOnlyDictionary<TKey, TValue> MergeNoConflictsAllowed<TKey, TValue>(this IEnumerable<IEnumerable<KeyValuePair<TKey, TValue>>> items)
		{
			Dictionary<TKey, TValue> dictionary = new Dictionary<TKey, TValue>();
			foreach (IEnumerable<KeyValuePair<TKey, TValue>> item in items)
			{
				foreach (KeyValuePair<TKey, TValue> item2 in item)
				{
					dictionary.Add(item2.Key, item2.Value);
				}
			}
			return dictionary.AsReadOnly();
		}
	}
}
