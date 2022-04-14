using System;
using System.Collections.Generic;
using System.Linq;

namespace Unity.IL2CPP
{
	public class HashCodeCache<T>
	{
		private readonly HashSet<string> _hashes = new HashSet<string>();

		private readonly Dictionary<T, string> _cache;

		private readonly Func<T, string> _hashFunc;

		public int Count => _cache.Count;

		public HashCodeCache(Func<T, string> hashFunc)
			: this(hashFunc, (IEqualityComparer<T>)null)
		{
		}

		public HashCodeCache(Func<T, string> hashFunc, IEqualityComparer<T> comparer)
		{
			_cache = ((comparer != null) ? new Dictionary<T, string>(comparer) : new Dictionary<T, string>());
			_hashFunc = hashFunc;
		}

		public void Clear()
		{
			_hashes.Clear();
			_cache.Clear();
		}

		public string GetUniqueHash(T value)
		{
			if (_cache.TryGetValue(value, out var hash))
			{
				return hash;
			}
			hash = _hashFunc(value);
			if (!_hashes.Add(hash))
			{
				throw new HashCodeCollisionException(hash, _cache.Single((KeyValuePair<T, string> pair) => pair.Value == hash).Key.ToString(), value.ToString());
			}
			_cache.Add(value, hash);
			return hash;
		}
	}
}
