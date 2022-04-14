using System;
using System.Collections.Generic;
using System.Text;
using Mono.Cecil;
using Unity.IL2CPP.Contexts.Collectors;
using Unity.IL2CPP.Contexts.Components.Base;
using Unity.IL2CPP.Contexts.Forking.Steps;
using Unity.IL2CPP.Contexts.Results;
using Unity.IL2CPP.Diagnostics;

namespace Unity.IL2CPP.Contexts.Components
{
	public class CppDeclarationsComponent : StatefulComponentBase<ICppDeclarationsCacheWriter, ICppDeclarationsCache, CppDeclarationsComponent>, ICppDeclarationsCache, ICppDeclarationsCacheWriter, ICppIncludeDepthCalculatorCache
	{
		private class NotAvailable : ICppDeclarationsCacheWriter, ICppIncludeDepthCalculatorCache, ICppDeclarationsCache
		{
			public int GetOrCalculateDepth(TypeReference type)
			{
				throw new NotSupportedException();
			}

			public void PopulateCache(SourceWritingContext context, IEnumerable<TypeReference> rootTypes)
			{
				throw new NotSupportedException();
			}

			public ICppDeclarations GetDeclarations(TypeReference type)
			{
				throw new NotSupportedException();
			}

			public string GetSource(TypeReference type)
			{
				throw new NotSupportedException();
			}

			public bool TryGetValue(TypeReference type, out CppDeclarationsCache.CacheData data)
			{
				throw new NotSupportedException();
			}
		}

		private readonly CppDeclarationsCache _cache;

		private readonly CppIncludeDepthCalculator _comparer;

		public CppDeclarationsComponent()
		{
			_cache = new CppDeclarationsCache();
			_comparer = new CppIncludeDepthCalculator(_cache);
		}

		private CppDeclarationsComponent(CppDeclarationsComponent parent)
		{
			_cache = new CppDeclarationsCache(parent._cache);
			_comparer = new CppIncludeDepthCalculator(_cache, parent._comparer);
		}

		void ICppDeclarationsCacheWriter.PopulateCache(SourceWritingContext context, IEnumerable<TypeReference> rootTypes)
		{
			CppDeclarationsCollector.PopulateCache(context, rootTypes, _cache);
		}

		int ICppIncludeDepthCalculatorCache.GetOrCalculateDepth(TypeReference type)
		{
			return _comparer.GetOrCalculateDepth(type);
		}

		ICppDeclarations ICppDeclarationsCache.GetDeclarations(TypeReference type)
		{
			return _cache.GetDeclarations(type);
		}

		string ICppDeclarationsCache.GetSource(TypeReference type)
		{
			return _cache.GetSource(type);
		}

		bool ICppDeclarationsCache.TryGetValue(TypeReference type, out CppDeclarationsCache.CacheData data)
		{
			return _cache.TryGetValue(type, out data);
		}

		protected override void DumpState(StringBuilder builder)
		{
			((IDumpableState)_cache).DumpState(builder);
			((IDumpableState)_comparer).DumpState(builder);
		}

		protected override void HandleMergeForAdd(CppDeclarationsComponent forked)
		{
			_cache.Merge(forked._cache);
			_comparer.Merge(forked._comparer);
		}

		protected override void HandleMergeForMergeValues(CppDeclarationsComponent forked)
		{
			throw new NotImplementedException();
		}

		protected override CppDeclarationsComponent CreateEmptyInstance()
		{
			return new CppDeclarationsComponent();
		}

		protected override CppDeclarationsComponent CreateCopyInstance()
		{
			return new CppDeclarationsComponent(this);
		}

		protected override CppDeclarationsComponent ThisAsFull()
		{
			return this;
		}

		protected override ICppDeclarationsCache ThisAsRead()
		{
			return this;
		}

		protected override ICppDeclarationsCacheWriter GetNotAvailableWrite()
		{
			return new NotAvailable();
		}

		protected override ICppDeclarationsCache GetNotAvailableRead()
		{
			return new NotAvailable();
		}

		protected override void ForkForSecondaryCollection(SecondaryCollectionLateAccessForkingContainer lateAccess, out ICppDeclarationsCacheWriter writer, out ICppDeclarationsCache reader, out CppDeclarationsComponent full)
		{
			NotAvailableFork(out writer, out reader, out full);
		}

		protected override void ForkForPrimaryWrite(PrimaryWriteAssembliesLateAccessForkingContainer lateAccess, out ICppDeclarationsCacheWriter writer, out ICppDeclarationsCache reader, out CppDeclarationsComponent full)
		{
			((ComponentBase<ICppDeclarationsCacheWriter, ICppDeclarationsCache, CppDeclarationsComponent>)this).ReadWriteFork(out writer, out reader, out full, ForkMode.Empty, MergeMode.Add);
		}

		protected override void ForkForPrimaryCollection(PrimaryCollectionLateAccessForkingContainer lateAccess, out ICppDeclarationsCacheWriter writer, out ICppDeclarationsCache reader, out CppDeclarationsComponent full)
		{
			NotAvailableFork(out writer, out reader, out full);
		}

		protected override void ForkForSecondaryWrite(SecondaryWriteLateAccessForkingContainer lateAccess, out ICppDeclarationsCacheWriter writer, out ICppDeclarationsCache reader, out CppDeclarationsComponent full)
		{
			((ComponentBase<ICppDeclarationsCacheWriter, ICppDeclarationsCache, CppDeclarationsComponent>)this).ReadWriteFork(out writer, out reader, out full, ForkMode.Copy, MergeMode.None);
		}
	}
}
