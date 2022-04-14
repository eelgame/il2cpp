using Unity.IL2CPP.AssemblyConversion;

namespace Unity.IL2CPP.Contexts.Forking.Providers
{
	public interface IUnrestrictedContextDataProvider
	{
		AssemblyConversionParameters Parameters { get; }

		AssemblyConversionInputData InputData { get; }

		AssemblyConversionResults PhaseResults { get; }

		IUnrestrictedContextCollectorProvider Collectors { get; }

		IUnrestrictedContextServicesProvider Services { get; }

		IUnrestrictedContextStatefulServicesProvider StatefulServices { get; }
	}
}
