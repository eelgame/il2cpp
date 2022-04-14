using System.Collections.Generic;
using System.Text;
using Mono.Cecil;
using Unity.Cecil.Awesome.Comparers;
using Unity.Cecil.Awesome.Ordering;
using Unity.IL2CPP.Contexts.Results;
using Unity.IL2CPP.Diagnostics;

namespace Unity.IL2CPP
{
	public class CppDeclarationsCache : ICppDeclarationsCache, IDumpableState
	{
		public class CacheData
		{
			public ICppDeclarations declarations;

			public string definition;
		}

		private Dictionary<TypeReference, CacheData> _cache;

		public CppDeclarationsCache()
		{
			_cache = new Dictionary<TypeReference, CacheData>(new TypeReferenceEqualityComparer());
		}

		public CppDeclarationsCache(CppDeclarationsCache parent)
		{
			_cache = new Dictionary<TypeReference, CacheData>(parent._cache, new TypeReferenceEqualityComparer());
		}

		public void Add(TypeReference type, CacheData data)
		{
			_cache.Add(type, data);
		}

		public bool TryGetValue(TypeReference type, out CacheData data)
		{
			return _cache.TryGetValue(type, out data);
		}

		public ICppDeclarations GetDeclarations(TypeReference type)
		{
			return _cache[type].declarations;
		}

		public string GetSource(TypeReference type)
		{
			return _cache[type].definition;
		}

		public void Merge(CppDeclarationsCache forked)
		{
			foreach (KeyValuePair<TypeReference, CacheData> item in forked._cache)
			{
				_cache[item.Key] = item.Value;
			}
		}

		void IDumpableState.DumpState(StringBuilder builder)
		{
			CollectorStateDumper.AppendCollection(builder, "_cache", _cache.Keys.ToSortedCollection());
		}
	}
}
