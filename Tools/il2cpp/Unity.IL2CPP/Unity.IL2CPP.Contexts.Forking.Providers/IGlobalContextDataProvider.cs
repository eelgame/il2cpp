using Unity.IL2CPP.AssemblyConversion;

namespace Unity.IL2CPP.Contexts.Forking.Providers
{
	public interface IGlobalContextDataProvider
	{
		AssemblyConversionParameters Parameters { get; }

		AssemblyConversionInputData InputData { get; }

		IGlobalContextCollectorProvider Collectors { get; }

		IGlobalContextServicesProvider Services { get; }

		IGlobalContextStatefulServicesProvider StatefulServices { get; }

		IGlobalContextResultsProvider Results { get; }

		IGlobalContextPhaseResultsProvider PhaseResults { get; }
	}
}
