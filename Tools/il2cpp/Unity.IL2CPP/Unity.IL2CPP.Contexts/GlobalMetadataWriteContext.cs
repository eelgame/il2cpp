using System;
using Unity.IL2CPP.AssemblyConversion;
using Unity.IL2CPP.Contexts.Collectors;
using Unity.IL2CPP.Contexts.Forking.Providers;
using Unity.IL2CPP.Contexts.Services;

namespace Unity.IL2CPP.Contexts
{
	public class GlobalMetadataWriteContext
	{
		public class ContextCollectors
		{
			public readonly IStatsWriterService Stats;

			public readonly IVTableBuilder VTable;

			public ContextCollectors(IGlobalContextCollectorProvider collectorProvider)
			{
				Stats = collectorProvider.Stats;
				VTable = collectorProvider.VTable;
			}
		}

		public class ContextResults
		{
			private readonly IGlobalContextPhaseResultsProvider _phaseResults;

			public AssemblyConversionResults.SecondaryCollectionPhase SecondaryCollection => _phaseResults.SecondaryCollection;

			public AssemblyConversionResults.PrimaryCollectionPhase PrimaryCollection => _phaseResults.PrimaryCollection;

			public AssemblyConversionResults.PrimaryWritePhase PrimaryWrite => _phaseResults.PrimaryWrite;

			public AssemblyConversionResults.SecondaryWritePhasePart1 SecondaryWritePart1 => _phaseResults.SecondaryWritePart1;

			public AssemblyConversionResults.SecondaryWritePhasePart3 SecondaryWritePart3 => _phaseResults.SecondaryWritePart3;

			public AssemblyConversionResults.SecondaryWritePhase SecondaryWrite => _phaseResults.SecondaryWrite;

			public ContextResults(IGlobalContextPhaseResultsProvider phaseResults)
			{
				_phaseResults = phaseResults;
			}
		}

		public class ContextServices
		{
			public readonly INamingService Naming;

			public readonly IObjectFactory Factory;

			public readonly IWindowsRuntimeProjections WindowsRuntime;

			public ContextServices(IGlobalContextServicesProvider servicesProvider, IGlobalContextStatefulServicesProvider statefulServicesProvider)
			{
				Naming = statefulServicesProvider.Naming;
				Factory = servicesProvider.Factory;
				WindowsRuntime = servicesProvider.WindowsRuntime;
			}
		}

		private readonly GlobalReadOnlyContext _globalReadOnlyContext;

		public readonly ContextCollectors Collectors;

		public readonly ContextServices Services;

		public readonly ContextResults Results;

		public readonly AssemblyConversionInputData InputData;

		public readonly AssemblyConversionParameters Parameters;

		public GlobalMetadataWriteContext(AssemblyConversionContext assemblyConversionContext)
			: this(assemblyConversionContext.ContextDataProvider, assemblyConversionContext.GlobalReadOnlyContext)
		{
		}

		public GlobalMetadataWriteContext(IGlobalContextDataProvider provider, GlobalReadOnlyContext globalReadOnlyContext)
		{
			if (globalReadOnlyContext == null)
			{
				throw new ArgumentNullException("globalReadOnlyContext");
			}
			_globalReadOnlyContext = globalReadOnlyContext;
			Collectors = new ContextCollectors(provider.Collectors);
			Services = new ContextServices(provider.Services, provider.StatefulServices);
			Results = new ContextResults(provider.PhaseResults);
			Parameters = provider.Parameters;
			InputData = provider.InputData;
		}

		public GlobalReadOnlyContext AsReadOnly()
		{
			return _globalReadOnlyContext;
		}

		public ReadOnlyContext GetReadOnlyContext()
		{
			return AsReadOnly().GetReadOnlyContext();
		}

		public MetadataWriteContext CreateWriteContext()
		{
			return new MetadataWriteContext(this);
		}
	}
}
