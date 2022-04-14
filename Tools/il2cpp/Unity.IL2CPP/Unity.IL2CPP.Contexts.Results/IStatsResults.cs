using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Unity.IL2CPP.Contexts.Results
{
	public interface IStatsResults
	{
		long ConversionMilliseconds { get; }

		int JobCount { get; }

		int FilesWritten { get; }

		int TypesConverted { get; }

		int StringLiterals { get; }

		int Methods { get; }

		int GenericTypeMethods { get; }

		int GenericMethods { get; }

		int ShareableMethods { get; }

		int TailCallsEncountered { get; }

		int WindowsRuntimeBoxedTypes { get; }

		int WindowsRuntimeTypesWithNames { get; }

		int NativeToManagedInterfaceAdapters { get; }

		int ComCallableWrappers { get; }

		int ArrayComCallableWrappers { get; }

		int ImplementedComCallableWrapperMethods { get; }

		int StrippedComCallableWrapperMethods { get; }

		int ForwardedToBaseClassComCallableWrapperMethods { get; }

		long MetadataTotal { get; }

		int TotalNullChecks { get; }

		ReadOnlyDictionary<string, long> MetadataStreams { get; }

		Dictionary<string, int> NullCheckMethodsCount { get; }

		HashSet<string> NullChecksMethods { get; }

		HashSet<string> ArrayBoundsChecksMethods { get; }

		HashSet<string> DivideByZeroChecksMethods { get; }

		HashSet<string> MemoryBarrierMethods { get; }

		HashSet<string> SharableMethods { get; }
	}
}
