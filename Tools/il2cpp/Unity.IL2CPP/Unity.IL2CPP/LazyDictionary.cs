using System;
using System.Collections.Generic;

namespace Unity.IL2CPP
{
	internal class LazyDictionary<K, V>
	{
		private readonly Func<V> _createValue;

		private Dictionary<K, V> _items = new Dictionary<K, V>();

		public V this[K key]
		{
			get
			{
				if (_items.TryGetValue(key, out var value))
				{
					return value;
				}
				_items.Add(key, value = _createValue());
				return value;
			}
		}

		public LazyDictionary(Func<V> createValue)
		{
			_createValue = createValue;
		}
	}
}
