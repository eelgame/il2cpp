using Unity.IL2CPP.AssemblyConversion;

namespace Unity.IL2CPP.Contexts.Forking.Providers
{
	public interface IGlobalContextPhaseResultsProvider
	{
		AssemblyConversionResults.PrimaryCollectionPhase PrimaryCollection { get; }

		AssemblyConversionResults.SecondaryCollectionPhase SecondaryCollection { get; }

		AssemblyConversionResults.SecondaryWritePhasePart1 SecondaryWritePart1 { get; }

		AssemblyConversionResults.SecondaryWritePhasePart3 SecondaryWritePart3 { get; }

		AssemblyConversionResults.SecondaryWritePhase SecondaryWrite { get; }

		AssemblyConversionResults.PrimaryWritePhase PrimaryWrite { get; }

		AssemblyConversionResults.SetupPhase Setup { get; }
	}
}
