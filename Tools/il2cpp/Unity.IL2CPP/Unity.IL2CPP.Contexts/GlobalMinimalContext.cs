using System;
using Unity.IL2CPP.AssemblyConversion;
using Unity.IL2CPP.Contexts.Collectors;
using Unity.IL2CPP.Contexts.Forking.Providers;
using Unity.IL2CPP.Contexts.Services;

namespace Unity.IL2CPP.Contexts
{
	public class GlobalMinimalContext
	{
		public class ContextCollectors
		{
			public readonly IStatsWriterService Stats;

			public ContextCollectors(IGlobalContextCollectorProvider collectorProvider)
			{
				Stats = collectorProvider.Stats;
			}
		}

		public class ContextResults
		{
		}

		public class ContextServices
		{
			public readonly INamingService Naming;

			public readonly IGuidProvider GuidProvider;

			public readonly ITypeProviderService TypeProvider;

			public readonly IObjectFactory Factory;

			public readonly IWindowsRuntimeProjections WindowsRuntime;

			public readonly IErrorInformationService ErrorInformation;

			public readonly IPathFactoryService PathFactory;

			public ContextServices(IGlobalContextServicesProvider servicesProvider, IGlobalContextStatefulServicesProvider statefulServicesProvider)
			{
				Naming = statefulServicesProvider.Naming;
				GuidProvider = servicesProvider.GuidProvider;
				TypeProvider = servicesProvider.TypeProvider;
				Factory = servicesProvider.Factory;
				WindowsRuntime = servicesProvider.WindowsRuntime;
				ErrorInformation = statefulServicesProvider.ErrorInformation;
				PathFactory = servicesProvider.PathFactory;
			}
		}

		private readonly GlobalReadOnlyContext _globalReadOnlyContext;

		public readonly ContextCollectors Collectors;

		public readonly ContextServices Services;

		public readonly ContextResults Results;

		public readonly AssemblyConversionInputData InputData;

		public readonly AssemblyConversionParameters Parameters;

		public GlobalMinimalContext(AssemblyConversionContext assemblyConversionContext)
			: this(assemblyConversionContext.ContextDataProvider, assemblyConversionContext.GlobalReadOnlyContext)
		{
		}

		public GlobalMinimalContext(IGlobalContextDataProvider provider, GlobalReadOnlyContext globalReadOnlyContext)
		{
			if (globalReadOnlyContext == null)
			{
				throw new ArgumentNullException("globalReadOnlyContext");
			}
			_globalReadOnlyContext = globalReadOnlyContext;
			Collectors = new ContextCollectors(provider.Collectors);
			Services = new ContextServices(provider.Services, provider.StatefulServices);
			Results = new ContextResults();
			Parameters = provider.Parameters;
			InputData = provider.InputData;
		}

		public GlobalReadOnlyContext AsReadOnly()
		{
			return _globalReadOnlyContext;
		}

		public MinimalContext CreateMinimalContext()
		{
			return new MinimalContext(this);
		}

		public ReadOnlyContext GetReadOnlyContext()
		{
			return AsReadOnly().GetReadOnlyContext();
		}
	}
}
