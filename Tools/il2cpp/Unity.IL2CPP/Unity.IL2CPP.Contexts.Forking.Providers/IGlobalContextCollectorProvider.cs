using Unity.IL2CPP.Contexts.Collectors;

namespace Unity.IL2CPP.Contexts.Forking.Providers
{
	public interface IGlobalContextCollectorProvider
	{
		IMethodCollector Methods { get; }

		ISharedMethodCollector SharedMethods { get; }

		IVirtualCallCollector VirtualCalls { get; }

		ISymbolsCollector Symbols { get; }

		IMetadataUsageCollectorWriterService MetadataUsage { get; }

		IStatsWriterService Stats { get; }

		ITypeCollector Types { get; }

		IGenericMethodCollector GenericMethods { get; }

		IRuntimeImplementedMethodWriterCollector RuntimeImplementedMethodWriters { get; }

		IVTableBuilder VTable { get; }

		ITinyTypeCollector TinyTypes { get; }

		ITinyStringCollector TinyStrings { get; }

		ICppDeclarationsCacheWriter CppDeclarationsCache { get; }

		ICppIncludeDepthCalculatorCache CppIncludeDepthCalculatorCache { get; }

		IMatchedAssemblyMethodSourceFilesCollector MatchedAssemblyMethodSourceFiles { get; }

		IReversePInvokeWrapperCollector ReversePInvokeWrappers { get; }

		IWindowsRuntimeTypeWithNameCollector WindowsRuntimeTypeWithNames { get; }

		ICCWMarshallingFunctionCollector CCWMarshallingFunctions { get; }

		IInteropGuidCollector InteropGuids { get; }

		ITypeMarshallingFunctionsCollector TypeMarshallingFunctions { get; }

		IWrapperForDelegateFromManagedToNativeCollector WrappersForDelegateFromManagedToNative { get; }
	}
}
