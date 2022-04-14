using Unity.IL2CPP.Contexts.Components;
using Unity.IL2CPP.Metadata;
using Unity.IL2CPP.Symbols;
using Unity.IL2CPP.Tiny;

namespace Unity.IL2CPP.Contexts.Forking.Providers
{
	public interface IUnrestrictedContextCollectorProvider
	{
		MetadataUsageCollectorComponent MetadataUsage { get; }

		MethodCollector Methods { get; }

		SymbolsCollector Symbols { get; }

		VirtualCallCollector VirtualCalls { get; }

		SharedMethodCollector SharedMethods { get; }

		TypeCollector TypeCollector { get; }

		GenericMethodCollectorComponent GenericMethodCollector { get; }

		TinyStringCollectorComponent TinyStringCollector { get; }

		TinyTypeCollectorComponent TinyTypeCollector { get; }

		RuntimeImplementedMethodWriterCollectorComponent RuntimeImplementedMethodWriterCollector { get; }

		StatsComponent Stats { get; }

		VTableBuilder VTableBuilder { get; }

		CppDeclarationsComponent CppDeclarations { get; }

		MatchedAssemblyMethodSourceFilesComponent MatchedAssemblyMethodSourceFiles { get; }

		ReversePInvokeWrapperComponent ReversePInvokeWrappers { get; }

		WindowsRuntimeTypeWithNameComponent WindowsRuntimeTypeWithNames { get; }

		CCWMarshallingFunctionComponent CCWMarshallingFunctions { get; }

		InteropGuidComponent InteropGuids { get; }

		TypeMarshallingFunctionsComponent TypeMarshallingFunctions { get; }

		WrapperForDelegateFromManagedToNativeComponent WrappersForDelegateFromManagedToNative { get; }
	}
}
