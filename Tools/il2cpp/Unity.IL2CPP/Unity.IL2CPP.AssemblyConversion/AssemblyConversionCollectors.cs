using Unity.IL2CPP.Contexts.Collectors;
using Unity.IL2CPP.Contexts.Components;
using Unity.IL2CPP.Contexts.Forking.Providers;
using Unity.IL2CPP.Contexts.Results;
using Unity.IL2CPP.Metadata;
using Unity.IL2CPP.Symbols;
using Unity.IL2CPP.Tiny;

namespace Unity.IL2CPP.AssemblyConversion
{
	public class AssemblyConversionCollectors : IGlobalContextCollectorProvider, IGlobalContextResultsProvider, IUnrestrictedContextCollectorProvider
	{
		public readonly MetadataUsageCollectorComponent MetadataUsage = new MetadataUsageCollectorComponent();

		public readonly MethodCollector Methods = new MethodCollector();

		public readonly SymbolsCollector Symbols = new SymbolsCollector();

		public readonly VirtualCallCollector VirtualCalls = new VirtualCallCollector();

		public readonly SharedMethodCollector SharedMethods = new SharedMethodCollector();

		public readonly TypeCollector TypeCollector = new TypeCollector();

		public readonly ReversePInvokeWrapperComponent ReversePInvokeWrappers = new ReversePInvokeWrapperComponent();

		public readonly WindowsRuntimeTypeWithNameComponent WindowsRuntimeTypeWithNames = new WindowsRuntimeTypeWithNameComponent();

		public readonly CCWMarshallingFunctionComponent CCWMarshallingFunctions = new CCWMarshallingFunctionComponent();

		public readonly InteropGuidComponent InteropGuids = new InteropGuidComponent();

		public readonly TypeMarshallingFunctionsComponent TypeMarshallingFunctions = new TypeMarshallingFunctionsComponent();

		public readonly WrapperForDelegateFromManagedToNativeComponent WrappersForDelegateFromManagedToNative = new WrapperForDelegateFromManagedToNativeComponent();

		public readonly GenericMethodCollectorComponent GenericMethodCollector = new GenericMethodCollectorComponent();

		public readonly TinyStringCollectorComponent TinyStringCollector = new TinyStringCollectorComponent();

		public readonly TinyTypeCollectorComponent TinyTypeCollector = new TinyTypeCollectorComponent();

		public readonly RuntimeImplementedMethodWriterCollectorComponent RuntimeImplementedMethodWriterCollector = new RuntimeImplementedMethodWriterCollectorComponent();

		public readonly StatsComponent Stats = new StatsComponent();

		public readonly VTableBuilder VTableBuilder = new VTableBuilder();

		public readonly CppDeclarationsComponent CppDeclarations = new CppDeclarationsComponent();

		public readonly MatchedAssemblyMethodSourceFilesComponent MatchedAssemblyMethodSourceFiles = new MatchedAssemblyMethodSourceFilesComponent();

		MetadataUsageCollectorComponent IUnrestrictedContextCollectorProvider.MetadataUsage => MetadataUsage;

		MethodCollector IUnrestrictedContextCollectorProvider.Methods => Methods;

		SymbolsCollector IUnrestrictedContextCollectorProvider.Symbols => Symbols;

		VirtualCallCollector IUnrestrictedContextCollectorProvider.VirtualCalls => VirtualCalls;

		SharedMethodCollector IUnrestrictedContextCollectorProvider.SharedMethods => SharedMethods;

		TypeCollector IUnrestrictedContextCollectorProvider.TypeCollector => TypeCollector;

		GenericMethodCollectorComponent IUnrestrictedContextCollectorProvider.GenericMethodCollector => GenericMethodCollector;

		TinyStringCollectorComponent IUnrestrictedContextCollectorProvider.TinyStringCollector => TinyStringCollector;

		TinyTypeCollectorComponent IUnrestrictedContextCollectorProvider.TinyTypeCollector => TinyTypeCollector;

		RuntimeImplementedMethodWriterCollectorComponent IUnrestrictedContextCollectorProvider.RuntimeImplementedMethodWriterCollector => RuntimeImplementedMethodWriterCollector;

		StatsComponent IUnrestrictedContextCollectorProvider.Stats => Stats;

		VTableBuilder IUnrestrictedContextCollectorProvider.VTableBuilder => VTableBuilder;

		CppDeclarationsComponent IUnrestrictedContextCollectorProvider.CppDeclarations => CppDeclarations;

		MatchedAssemblyMethodSourceFilesComponent IUnrestrictedContextCollectorProvider.MatchedAssemblyMethodSourceFiles => MatchedAssemblyMethodSourceFiles;

		ReversePInvokeWrapperComponent IUnrestrictedContextCollectorProvider.ReversePInvokeWrappers => ReversePInvokeWrappers;

		WindowsRuntimeTypeWithNameComponent IUnrestrictedContextCollectorProvider.WindowsRuntimeTypeWithNames => WindowsRuntimeTypeWithNames;

		CCWMarshallingFunctionComponent IUnrestrictedContextCollectorProvider.CCWMarshallingFunctions => CCWMarshallingFunctions;

		InteropGuidComponent IUnrestrictedContextCollectorProvider.InteropGuids => InteropGuids;

		TypeMarshallingFunctionsComponent IUnrestrictedContextCollectorProvider.TypeMarshallingFunctions => TypeMarshallingFunctions;

		WrapperForDelegateFromManagedToNativeComponent IUnrestrictedContextCollectorProvider.WrappersForDelegateFromManagedToNative => WrappersForDelegateFromManagedToNative;

		IInteropGuidCollector IGlobalContextCollectorProvider.InteropGuids => InteropGuids;

		ITypeMarshallingFunctionsCollector IGlobalContextCollectorProvider.TypeMarshallingFunctions => TypeMarshallingFunctions;

		IWrapperForDelegateFromManagedToNativeCollector IGlobalContextCollectorProvider.WrappersForDelegateFromManagedToNative => WrappersForDelegateFromManagedToNative;

		ICCWMarshallingFunctionCollector IGlobalContextCollectorProvider.CCWMarshallingFunctions => CCWMarshallingFunctions;

		IReversePInvokeWrapperCollector IGlobalContextCollectorProvider.ReversePInvokeWrappers => ReversePInvokeWrappers;

		IWindowsRuntimeTypeWithNameCollector IGlobalContextCollectorProvider.WindowsRuntimeTypeWithNames => WindowsRuntimeTypeWithNames;

		IMethodCollector IGlobalContextCollectorProvider.Methods => Methods;

		ISharedMethodCollector IGlobalContextCollectorProvider.SharedMethods => SharedMethods;

		IVirtualCallCollector IGlobalContextCollectorProvider.VirtualCalls => VirtualCalls;

		ISymbolsCollector IGlobalContextCollectorProvider.Symbols => Symbols;

		IMetadataUsageCollectorWriterService IGlobalContextCollectorProvider.MetadataUsage => MetadataUsage;

		IStatsWriterService IGlobalContextCollectorProvider.Stats => Stats;

		ITypeCollector IGlobalContextCollectorProvider.Types => TypeCollector;

		IGenericMethodCollector IGlobalContextCollectorProvider.GenericMethods => GenericMethodCollector;

		IRuntimeImplementedMethodWriterCollector IGlobalContextCollectorProvider.RuntimeImplementedMethodWriters => RuntimeImplementedMethodWriterCollector;

		IVTableBuilder IGlobalContextCollectorProvider.VTable => VTableBuilder;

		ITinyTypeCollector IGlobalContextCollectorProvider.TinyTypes => TinyTypeCollector;

		ICppDeclarationsCache IGlobalContextResultsProvider.CppDeclarationsCache => CppDeclarations;

		ICppDeclarationsCacheWriter IGlobalContextCollectorProvider.CppDeclarationsCache => CppDeclarations;

		ICppIncludeDepthCalculatorCache IGlobalContextCollectorProvider.CppIncludeDepthCalculatorCache => CppDeclarations;

		IMatchedAssemblyMethodSourceFilesCollector IGlobalContextCollectorProvider.MatchedAssemblyMethodSourceFiles => MatchedAssemblyMethodSourceFiles;

		ITinyStringCollector IGlobalContextCollectorProvider.TinyStrings => TinyStringCollector;
	}
}
