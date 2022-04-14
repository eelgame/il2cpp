using System;
using Unity.IL2CPP.AssemblyConversion;
using Unity.IL2CPP.Contexts.Collectors;
using Unity.IL2CPP.Contexts.Forking.Providers;
using Unity.IL2CPP.Contexts.Services;

namespace Unity.IL2CPP.Contexts
{
	public class TinyGlobalWriteContext
	{
		public class ContextCollectors
		{
			public readonly ITinyStringCollector TinyStrings;

			public readonly ITinyTypeCollector TinyTypes;

			public ContextCollectors(IGlobalContextCollectorProvider collectorProvider)
			{
				TinyStrings = collectorProvider.TinyStrings;
				TinyTypes = collectorProvider.TinyTypes;
			}
		}

		public class ContextResults
		{
			private readonly IGlobalContextPhaseResultsProvider _phaseResults;

			public AssemblyConversionResults.PrimaryCollectionPhase PrimaryCollection => _phaseResults.PrimaryCollection;

			public AssemblyConversionResults.SecondaryCollectionPhase SecondaryCollection => _phaseResults.SecondaryCollection;

			public AssemblyConversionResults.PrimaryWritePhase PrimaryWrite => _phaseResults.PrimaryWrite;

			public ContextResults(IGlobalContextPhaseResultsProvider phaseResults)
			{
				_phaseResults = phaseResults;
			}
		}

		public class ContextServices
		{
			public readonly INamingService Naming;

			public readonly ITypeProviderService TypeProvider;

			public readonly IPathFactoryService PathFactory;

			public ContextServices(IGlobalContextServicesProvider servicesProvider, IGlobalContextStatefulServicesProvider statefulServicesProvider)
			{
				Naming = statefulServicesProvider.Naming;
				TypeProvider = servicesProvider.TypeProvider;
				PathFactory = servicesProvider.PathFactory;
			}
		}

		private readonly GlobalMinimalContext _globalMinimalContext;

		private readonly GlobalReadOnlyContext _globalReadOnlyContext;

		private readonly GlobalWriteContext _globalWriteContext;

		public readonly ContextCollectors Collectors;

		public readonly ContextServices Services;

		public readonly ContextResults Results;

		public readonly AssemblyConversionInputData InputData;

		public readonly AssemblyConversionParameters Parameters;

		public TinyGlobalWriteContext(AssemblyConversionContext assemblyConversionContext)
			: this(assemblyConversionContext.ContextDataProvider, assemblyConversionContext.GlobalWriteContext, assemblyConversionContext.GlobalMinimalContext, assemblyConversionContext.GlobalReadOnlyContext)
		{
		}

		public TinyGlobalWriteContext(IGlobalContextDataProvider provider, GlobalWriteContext globalWriteContext, GlobalMinimalContext globalMinimalContext, GlobalReadOnlyContext globalReadOnlyContext)
		{
			if (provider == null)
			{
				throw new ArgumentNullException("provider");
			}
			if (globalReadOnlyContext == null)
			{
				throw new ArgumentNullException("globalReadOnlyContext");
			}
			if (globalMinimalContext == null)
			{
				throw new ArgumentNullException("globalMinimalContext");
			}
			if (globalWriteContext == null)
			{
				throw new ArgumentNullException("globalWriteContext");
			}
			_globalWriteContext = globalWriteContext;
			_globalMinimalContext = globalMinimalContext;
			_globalReadOnlyContext = globalReadOnlyContext;
			Collectors = new ContextCollectors(provider.Collectors);
			Services = new ContextServices(provider.Services, provider.StatefulServices);
			Results = new ContextResults(provider.PhaseResults);
			Parameters = provider.Parameters;
			InputData = provider.InputData;
		}

		public GlobalMinimalContext AsMinimal()
		{
			return _globalMinimalContext;
		}

		public GlobalReadOnlyContext AsReadOnly()
		{
			return _globalReadOnlyContext;
		}

		public MinimalContext CreateMinimalContext()
		{
			return AsMinimal().CreateMinimalContext();
		}

		public ReadOnlyContext GetReadOnlyContext()
		{
			return AsReadOnly().GetReadOnlyContext();
		}

		public TinyWriteContext CreateWriteContext()
		{
			return new TinyWriteContext(this);
		}

		public GlobalWriteContext AsBig()
		{
			return _globalWriteContext;
		}
	}
}
