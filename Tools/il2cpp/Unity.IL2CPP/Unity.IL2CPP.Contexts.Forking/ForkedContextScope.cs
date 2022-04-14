using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Unity.IL2CPP.Contexts.Forking.Providers;
using Unity.MiniProfiling;

namespace Unity.IL2CPP.Contexts.Forking
{
	public class ForkedContextScope<TItem, TContext> : IDisposable
	{
		public struct Data
		{
			public readonly TItem Value;

			public readonly int Index;

			private readonly Func<int, TContext> _onDemandContext;

			private TContext _context;

			public TContext Context
			{
				get
				{
					if (_context == null)
					{
						_context = _onDemandContext(Index);
					}
					return _context;
				}
			}

			public Data(TItem value, TContext context, int index)
			{
				Value = value;
				_context = context;
				Index = index;
				_onDemandContext = null;
			}

			public Data(TItem value, Func<int, TContext> onDemandContext, int index)
			{
				Value = value;
				_context = default(TContext);
				Index = index;
				_onDemandContext = onDemandContext;
			}
		}

		private Dictionary<object, Action[]> _mergeBack = new Dictionary<object, Action[]>();

		private Dictionary<object, Func<IDataForker<TContext>, int, Action>> _forkDataProviderSetupTable = new Dictionary<object, Func<IDataForker<TContext>, int, Action>>();

		private List<IDataForker<TContext>> _forkDataProviders = new List<IDataForker<TContext>>();

		private Dictionary<TItem, TContext> _forkedContexts = new Dictionary<TItem, TContext>();

		private readonly int _count;

		private readonly ReadOnlyCollection<TItem> _items;

		private readonly bool _useParallelMerge;

		private readonly ForkCreationMode _creationMode;

		private readonly IPhaseResultsSetter<TContext> _phaseResultsSetter;

		public ReadOnlyCollection<Data> Items
		{
			get
			{
				List<Data> list = new List<Data>();
				foreach (TItem item in _items)
				{
					if (_forkedContexts[item] != null)
					{
						list.Add(new Data(item, _forkedContexts[item], list.Count));
					}
					else
					{
						list.Add(new Data(item, OnDemandContextAccess, list.Count));
					}
				}
				return list.AsReadOnly();
			}
		}

		private ForkedContextScope(ReadOnlyCollection<TItem> items, bool useParallelMerge, ForkCreationMode creationMode)
		{
			_items = items;
			_count = items.Count;
			_useParallelMerge = useParallelMerge;
			_creationMode = creationMode;
		}

		public ForkedContextScope(IUnrestrictedContextDataProvider context, ReadOnlyCollection<TItem> items, Func<IUnrestrictedContextDataProvider, IDataForker<TContext>> providerFactory, OverrideObjects overrideObjects, bool useParallelMerge = true, ForkCreationMode creationMode = ForkCreationMode.OnDemand)
			: this(items, useParallelMerge, creationMode)
		{
			Setup(context, providerFactory, overrideObjects, items.Count);
		}

		public ForkedContextScope(IUnrestrictedContextDataProvider context, ReadOnlyCollection<TItem> items, Func<IUnrestrictedContextDataProvider, IDataForker<TContext>> providerFactory, bool useParallelMerge = true, ForkCreationMode creationMode = ForkCreationMode.OnDemand)
			: this(items, useParallelMerge, creationMode)
		{
			Setup(context, providerFactory, null);
		}

		public ForkedContextScope(IUnrestrictedContextDataProvider context, ReadOnlyCollection<TItem> items, Func<IUnrestrictedContextDataProvider, IDataForker<TContext>> providerFactory, ReadOnlyCollection<OverrideObjects> overrideObjects, bool useParallelMerge = true, ForkCreationMode creationMode = ForkCreationMode.OnDemand, IPhaseResultsSetter<TContext> phaseResultsSetter = null)
			: this(items, useParallelMerge, creationMode)
		{
			_phaseResultsSetter = phaseResultsSetter;
			Setup(context, providerFactory, overrideObjects);
		}

		private void Setup(IUnrestrictedContextDataProvider context, Func<IUnrestrictedContextDataProvider, IDataForker<TContext>> providerFactory, OverrideObjects overrideObject, int count)
		{
			List<OverrideObjects> list = new List<OverrideObjects>();
			for (int i = 0; i < count; i++)
			{
				list.Add(overrideObject);
			}
			Setup(context, providerFactory, list.AsReadOnly());
		}

		private void Setup(IUnrestrictedContextDataProvider context, Func<IUnrestrictedContextDataProvider, IDataForker<TContext>> providerFactory, ReadOnlyCollection<OverrideObjects> overrideObjects)
		{
			using (MiniProfiler.Section("ForkedContextScope.Setup"))
			{
				CreateForkedDataProviders(context, providerFactory);
				SetupMergeEntries(context, overrideObjects);
				PopulateProvidersWithAllForks();
				PopulateContextTable();
			}
		}

		private void SetupMergeEntries(IUnrestrictedContextDataProvider context, ReadOnlyCollection<OverrideObjects> overrideObjects)
		{
			ForkingRegistration.SetupMergeEntries<TContext>(context, RegisterCollector, overrideObjects);
		}

		private void RegisterCollector(object collector, Func<IDataForker<TContext>, int, Action> fork)
		{
			_mergeBack[collector] = new Action[_count];
			_forkDataProviderSetupTable[collector] = fork;
		}

		private TContext OnDemandContextAccess(int index)
		{
			TItem key = _items[index];
			if (_forkedContexts.TryGetValue(key, out var value) && value != null)
			{
				return value;
			}
			foreach (object key2 in _mergeBack.Keys)
			{
				Action[] array = _mergeBack[key2];
				Func<IDataForker<TContext>, int, Action> func = _forkDataProviderSetupTable[key2];
				array[index] = func(_forkDataProviders[index], index);
			}
			value = _forkDataProviders[index].CreateForkedContext();
			_forkedContexts[key] = value;
			return value;
		}

		private void CreateForkedDataProviders(IUnrestrictedContextDataProvider context, Func<IUnrestrictedContextDataProvider, IDataForker<TContext>> providerFactory)
		{
			for (int i = 0; i < _count; i++)
			{
				_forkDataProviders.Add(providerFactory(context));
			}
		}

		private void PopulateContextTable()
		{
			if (_creationMode == ForkCreationMode.Upfront)
			{
				for (int i = 0; i < _count; i++)
				{
					_forkedContexts.Add(_items[i], _forkDataProviders[i].CreateForkedContext());
				}
			}
			else
			{
				for (int j = 0; j < _count; j++)
				{
					_forkedContexts.Add(_items[j], default(TContext));
				}
			}
		}

		private void PopulateProvidersWithAllForks()
		{
			if (_creationMode == ForkCreationMode.Upfront)
			{
				Parallel.ForEach(_mergeBack.Keys, PopulateForksInProvidersForObject);
				return;
			}
			foreach (object key in _mergeBack.Keys)
			{
				InitializeForksInProvidersForObject(key);
			}
		}

		private void InitializeForksInProvidersForObject(object obj)
		{
			Action[] array = _mergeBack[obj];
			for (int i = 0; i < _count; i++)
			{
				array[i] = null;
			}
		}

		private void PopulateForksInProvidersForObject(object obj)
		{
			Action[] array = _mergeBack[obj];
			Func<IDataForker<TContext>, int, Action> func = _forkDataProviderSetupTable[obj];
			for (int i = 0; i < _count; i++)
			{
				array[i] = func(_forkDataProviders[i], i);
			}
		}

		private void MergeBack()
		{
			using (MiniProfiler.Section("ForkedContextScope.MergeBack"))
			{
				if (_creationMode == ForkCreationMode.OnDemand && _forkedContexts.Values.All((TContext v) => v == null))
				{
					return;
				}
				if (_useParallelMerge)
				{
					Parallel.ForEach(_mergeBack, delegate(KeyValuePair<object, Action[]> pair)
					{
						MergeForObject(pair.Value, pair.Key.GetType());
					});
				}
				else
				{
					foreach (KeyValuePair<object, Action[]> item in _mergeBack)
					{
						MergeForObject(item.Value, item.Key.GetType());
					}
				}
				_phaseResultsSetter?.SetPhaseResults(_forkedContexts.Values.ToList().AsReadOnly());
			}
		}

		private void MergeForObject(Action[] merges, Type componentType)
		{
			using (MiniProfiler.Section("ForkedContextScope.MergingComponent", componentType.ToString()))
			{
				for (int i = 0; i < merges.Length; i++)
				{
					merges[i]?.Invoke();
				}
			}
		}

		public void Dispose()
		{
			MergeBack();
		}
	}
}
