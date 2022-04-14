using System;
using System.Collections.Generic;
using Unity.IL2CPP.Common;

namespace Unity.IL2CPP.GenericsCollection
{
	public class InflatedCollection<T>
	{
		private readonly HashSet<T> _items;

		public ReadOnlyHashSet<T> Items => _items.AsReadOnly();

		public int Count => _items.Count;

		public event Action<T> OnItemAdded;

		public InflatedCollection(IEqualityComparer<T> comparer)
		{
			_items = new HashSet<T>(comparer);
		}

		public virtual bool Add(T item)
		{
			bool num = _items.Add(item);
			if (num)
			{
				Action<T> onItemAdded = this.OnItemAdded;
				if (onItemAdded == null)
				{
					return num;
				}
				onItemAdded(item);
			}
			return num;
		}
	}
}
