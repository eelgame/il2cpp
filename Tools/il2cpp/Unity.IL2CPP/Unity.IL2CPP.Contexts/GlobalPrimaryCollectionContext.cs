using System;
using Unity.IL2CPP.AssemblyConversion;
using Unity.IL2CPP.Contexts.Collectors;
using Unity.IL2CPP.Contexts.Components;
using Unity.IL2CPP.Contexts.Forking;
using Unity.IL2CPP.Contexts.Forking.Providers;
using Unity.IL2CPP.Contexts.Services;

namespace Unity.IL2CPP.Contexts
{
	public class GlobalPrimaryCollectionContext
	{
		public class ContextCollectors
		{
			public readonly IStatsWriterService Stats;

			public readonly IVTableBuilder VTable;

			public readonly ITypeCollector Types;

			public readonly IGenericMethodCollector GenericMethods;

			public readonly IRuntimeImplementedMethodWriterCollector RuntimeImplementedMethodWriters;

			public readonly IWindowsRuntimeTypeWithNameCollector WindowsRuntimeTypeWithNames;

			public readonly ICCWMarshallingFunctionCollector CCWMarshallingFunctionCollector;

			public ContextCollectors(IGlobalContextCollectorProvider collectorProvider)
			{
				Stats = collectorProvider.Stats;
				VTable = collectorProvider.VTable;
				Types = collectorProvider.Types;
				GenericMethods = collectorProvider.GenericMethods;
				RuntimeImplementedMethodWriters = collectorProvider.RuntimeImplementedMethodWriters;
				WindowsRuntimeTypeWithNames = collectorProvider.WindowsRuntimeTypeWithNames;
				CCWMarshallingFunctionCollector = collectorProvider.CCWMarshallingFunctions;
			}
		}

		public class ContextResults
		{
			private readonly IGlobalContextPhaseResultsProvider _phaseResults;

			public AssemblyConversionResults.SetupPhase Setup => _phaseResults.Setup;

			public ContextResults(IGlobalContextPhaseResultsProvider phaseResults)
			{
				_phaseResults = phaseResults;
			}
		}

		public class ContextServices
		{
			public readonly INamingService Naming;

			public readonly ITypeProviderService TypeProvider;

			public readonly IWindowsRuntimeProjections WindowsRuntime;

			public readonly IErrorInformationService ErrorInformation;

			public readonly IWorkScheduler Scheduler;

			public readonly IMessageLogger MessageLogger;

			public ContextServices(IGlobalContextServicesProvider servicesProvider, IGlobalContextStatefulServicesProvider statefulServicesProvider)
			{
				Naming = statefulServicesProvider.Naming;
				TypeProvider = servicesProvider.TypeProvider;
				WindowsRuntime = servicesProvider.WindowsRuntime;
				ErrorInformation = statefulServicesProvider.ErrorInformation;
				Scheduler = statefulServicesProvider.Scheduler;
				MessageLogger = statefulServicesProvider.MessageLogger;
			}
		}

		private readonly IUnrestrictedContextDataProvider _parent;

		private readonly GlobalMinimalContext _globalMinimalContext;

		private readonly GlobalReadOnlyContext _globalReadOnlyContext;

		public readonly ContextCollectors Collectors;

		public readonly ContextServices Services;

		public readonly ContextResults Results;

		public readonly AssemblyConversionInputData InputData;

		public readonly AssemblyConversionParameters Parameters;

		public GlobalPrimaryCollectionContext(AssemblyConversionContext assemblyConversionContext)
			: this(assemblyConversionContext.ContextDataProvider, assemblyConversionContext.ContextDataProvider, assemblyConversionContext.GlobalMinimalContext, assemblyConversionContext.GlobalReadOnlyContext)
		{
		}

		public GlobalPrimaryCollectionContext(IUnrestrictedContextDataProvider parent, IGlobalContextDataProvider provider, GlobalMinimalContext globalMinimalContext, GlobalReadOnlyContext globalReadOnlyContext)
		{
			if (parent == null)
			{
				throw new ArgumentNullException("parent");
			}
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
			_parent = parent;
			_globalMinimalContext = globalMinimalContext;
			_globalReadOnlyContext = globalReadOnlyContext;
			Collectors = new ContextCollectors(provider.Collectors);
			Services = new ContextServices(provider.Services, provider.StatefulServices);
			Results = new ContextResults(provider.PhaseResults);
			Parameters = provider.Parameters;
			InputData = provider.InputData;
		}

		public ForkedContextScope<TItem, GlobalPrimaryCollectionContext> ForkFor<TItem>(Func<IUnrestrictedContextDataProvider, ForkedContextScope<TItem, GlobalPrimaryCollectionContext>> forker)
		{
			return forker(_parent);
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

		public PrimaryCollectionContext CreateCollectionContext()
		{
			return new PrimaryCollectionContext(this);
		}
	}
}
