using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using Mono.Cecil;
using Unity.Cecil.Awesome.Comparers;
using Unity.Cecil.Awesome.Ordering;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts.Collectors;
using Unity.IL2CPP.Diagnostics;

namespace Unity.IL2CPP
{
	public class CppIncludeDepthCalculator : ICppIncludeDepthCalculatorCache, IDumpableState
	{
		private readonly CppDeclarationsCache _cache;

		private readonly Dictionary<TypeReference, int> _depthCache;

		public ReadOnlyDictionary<TypeReference, int> Items => _depthCache.AsReadOnly();

		public CppIncludeDepthCalculator(CppDeclarationsCache cache)
		{
			_cache = cache;
			_depthCache = new Dictionary<TypeReference, int>(new TypeReferenceEqualityComparer());
		}

		public CppIncludeDepthCalculator(CppDeclarationsCache cache, CppIncludeDepthCalculator parentDepthCalculator)
		{
			_cache = cache;
			_depthCache = new Dictionary<TypeReference, int>(parentDepthCalculator._depthCache, new TypeReferenceEqualityComparer());
		}

		public int GetOrCalculateDepth(TypeReference type)
		{
			return GetOrCalculateDepthRecursive(type, new HashSet<TypeReference>(new TypeReferenceEqualityComparer()));
		}

		private int GetOrCalculateDepthRecursive(TypeReference type, HashSet<TypeReference> visitedTypes)
		{
			int value = 0;
			if (_depthCache.TryGetValue(type, out value))
			{
				return value;
			}
			if (visitedTypes.Contains(type))
			{
				return 0;
			}
			try
			{
				visitedTypes.Add(type);
				foreach (TypeReference dependency in CppDeclarationsCollector.GetDependencies(type, _cache))
				{
					value = Math.Max(value, 1 + GetOrCalculateDepthRecursive(dependency, visitedTypes));
				}
				_depthCache.Add(type, value);
				return value;
			}
			finally
			{
				visitedTypes.Remove(type);
			}
		}

		public void Merge(CppIncludeDepthCalculator forked)
		{
			foreach (KeyValuePair<TypeReference, int> item in forked._depthCache)
			{
				_depthCache[item.Key] = item.Value;
			}
		}

		void IDumpableState.DumpState(StringBuilder builder)
		{
			CollectorStateDumper.AppendCollection(builder, "_depthCache", _depthCache.Keys.ToSortedCollection());
		}
	}
}
