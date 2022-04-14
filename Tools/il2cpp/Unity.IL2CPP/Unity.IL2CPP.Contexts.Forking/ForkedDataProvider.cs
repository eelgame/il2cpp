using Unity.IL2CPP.AssemblyConversion;
using Unity.IL2CPP.Contexts.Forking.Providers;

namespace Unity.IL2CPP.Contexts.Forking
{
	public class ForkedDataProvider : IGlobalContextDataProvider, IUnrestrictedContextDataProvider
	{
		public readonly ForkedDataContainer Container;

		private readonly IUnrestrictedContextDataProvider _context;

		AssemblyConversionParameters IGlobalContextDataProvider.Parameters => _context.Parameters;

		AssemblyConversionInputData IUnrestrictedContextDataProvider.InputData => _context.InputData;

		AssemblyConversionResults IUnrestrictedContextDataProvider.PhaseResults => _context.PhaseResults;

		IUnrestrictedContextCollectorProvider IUnrestrictedContextDataProvider.Collectors => Container;

		IUnrestrictedContextServicesProvider IUnrestrictedContextDataProvider.Services => Container;

		IUnrestrictedContextStatefulServicesProvider IUnrestrictedContextDataProvider.StatefulServices => Container;

		AssemblyConversionParameters IUnrestrictedContextDataProvider.Parameters => _context.Parameters;

		AssemblyConversionInputData IGlobalContextDataProvider.InputData => _context.InputData;

		IGlobalContextCollectorProvider IGlobalContextDataProvider.Collectors => Container;

		IGlobalContextServicesProvider IGlobalContextDataProvider.Services => Container;

		IGlobalContextStatefulServicesProvider IGlobalContextDataProvider.StatefulServices => Container;

		IGlobalContextResultsProvider IGlobalContextDataProvider.Results => Container;

		IGlobalContextPhaseResultsProvider IGlobalContextDataProvider.PhaseResults => GetPhaseResults();

		public ForkedDataProvider(IUnrestrictedContextDataProvider context, ForkedDataContainer container)
		{
			_context = context;
			Container = container;
		}

		protected virtual IGlobalContextPhaseResultsProvider GetPhaseResults()
		{
			return _context.PhaseResults;
		}
	}
}
