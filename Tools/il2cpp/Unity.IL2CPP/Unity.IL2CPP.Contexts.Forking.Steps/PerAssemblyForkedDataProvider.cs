using Unity.IL2CPP.AssemblyConversion;
using Unity.IL2CPP.Contexts.Forking.Providers;

namespace Unity.IL2CPP.Contexts.Forking.Steps
{
	public class PerAssemblyForkedDataProvider : ForkedDataProvider
	{
		private readonly AssemblyConversionResults _phaseResultsContainer;

		public PerAssemblyForkedDataProvider(IUnrestrictedContextDataProvider context, ForkedDataContainer container, AssemblyConversionResults phaseResultsContainer)
			: base(context, container)
		{
			_phaseResultsContainer = phaseResultsContainer;
		}

		protected override IGlobalContextPhaseResultsProvider GetPhaseResults()
		{
			return _phaseResultsContainer;
		}
	}
}
