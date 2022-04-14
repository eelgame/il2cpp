using System;
using Unity.IL2CPP.Contexts.Collectors;
using Unity.IL2CPP.Contexts.Components;
using Unity.IL2CPP.Contexts.Forking.Providers;
using Unity.IL2CPP.Contexts.Results;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.Metadata;
using Unity.IL2CPP.Naming;
using Unity.IL2CPP.Symbols;
using Unity.IL2CPP.Tiny;

namespace Unity.IL2CPP.Contexts.Forking
{
	public class ForkedDataContainer : IGlobalContextCollectorProvider, IGlobalContextResultsProvider, IGlobalContextServicesProvider, IGlobalContextStatefulServicesProvider, IUnrestrictedContextCollectorProvider, IUnrestrictedContextServicesProvider, IUnrestrictedContextStatefulServicesProvider
	{
		public MetadataUsageCollectorComponent MetadataUsage;

		public MethodCollector Methods;

		public SymbolsCollector Symbols;

		public VirtualCallCollector VirtualCalls;

		public SharedMethodCollector SharedMethods;

		public TypeCollector TypeCollector;

		public GenericMethodCollectorComponent GenericMethodCollector;

		public TinyStringCollectorComponent TinyStringCollector;

		public TinyTypeCollectorComponent TinyTypeCollector;

		public RuntimeImplementedMethodWriterCollectorComponent RuntimeImplementedMethodWriterCollector;

		public StatsComponent Stats;

		public VTableBuilder VTableBuilder;

		public CppDeclarationsComponent CppDeclarations;

		public MatchedAssemblyMethodSourceFilesComponent MatchedAssemblyMethodSourceFiles;

		public ReversePInvokeWrapperComponent ReversePInvokeWrappers;

		public WindowsRuntimeTypeWithNameComponent WindowsRuntimeTypeWithNames;

		public CCWMarshallingFunctionComponent CcwMarshallingFunctions;

		public InteropGuidComponent InteropGuids;

		public TypeMarshallingFunctionsComponent TypeMarshallingFunctions;

		public WrapperForDelegateFromManagedToNativeComponent WrappersForDelegateFromManagedToNative;

		public IMethodCollector CollectorMethods;

		public ISharedMethodCollector CollectorSharedMethods;

		public IVirtualCallCollector CollectorVirtualCalls;

		public ISymbolsCollector CollectorSymbols;

		public IMetadataUsageCollectorWriterService CollectorMetadataUsage;

		public IStatsWriterService CollectorStats;

		public ITypeCollector CollectorTypes;

		public IGenericMethodCollector CollectorGenericMethods;

		public IVTableBuilder CollectorVTable;

		public ITinyStringCollector CollectorTinyStrings;

		public ITinyTypeCollector CollectorTinyTypes;

		public IRuntimeImplementedMethodWriterCollector CollectorRuntimeImplementedMethodWriter;

		public ICppDeclarationsCacheWriter CollectorCppDeclarationsCache;

		public ICppIncludeDepthCalculatorCache CollectorIncludeDepthCalculatorCache;

		public IMatchedAssemblyMethodSourceFilesCollector CollectorMatchedAssemblyMethodSourceFiles;

		public IReversePInvokeWrapperCollector CollectorReversePInvokeWrappers;

		public IWindowsRuntimeTypeWithNameCollector CollectorWindowsRuntimeTypeWithNames;

		public ICCWMarshallingFunctionCollector CollectorCcwMarshallingFunctions;

		public IInteropGuidCollector CollectorInteropGuids;

		public ITypeMarshallingFunctionsCollector CollectorTypeMarshallingFunctions;

		public IWrapperForDelegateFromManagedToNativeCollector CollectorWrappersForDelegateFromManagedToNative;

		public ICppDeclarationsCache ResultCppDeclarationsCache;

		public SourceAnnotationWriterComponent SourceAnnotationWriter;

		public NamingComponent Naming;

		public ErrorInformation ErrorInformation;

		public ImmediateSchedulerComponent Scheduler;

		public DiagnosticsComponent Diagnostics;

		public MessageLoggerComponent MessageLogger;

		public INamingService StatefulServicesNaming;

		public ISourceAnnotationWriter StatefulServicesSourceAnnotationWriter;

		public IErrorInformationService StatefulServicesErrorInformation;

		public IWorkScheduler StatefulServicesScheduler;

		public IDiagnosticsService StatefulServicesDiagnostics;

		public IMessageLogger StatefulServicesMessageLogger;

		public ICallMappingComponent ICallMapping;

		public GuidProviderComponent GuidProvider;

		public TypeProviderComponent TypeProvider;

		public AssemblyDependenciesComponent AssemblyDependencies;

		public WindowsRuntimeProjectionsComponent WindowsRuntimeProjections;

		public ObjectFactoryComponent Factory;

		public PathFactoryComponent PathFactory;

		public ContextScopeServiceComponent ContextScope;

		public IGuidProvider ServicesGuidProvider;

		public ITypeProviderService ServicesTypeProvider;

		public IObjectFactory ServicesFactory;

		public IWindowsRuntimeProjections ServicesWindowsRuntime;

		public IICallMappingService ServicesICallMapping;

		public IAssemblyDependencyResults ServicesAssemblyDependencies;

		public IPathFactoryService ServicesPathFactory;

		public IContextScopeService ServicesContextScopeService;

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

		AssemblyDependenciesComponent IUnrestrictedContextServicesProvider.AssemblyDependencies => AssemblyDependencies;

		PathFactoryComponent IUnrestrictedContextServicesProvider.PathFactory => PathFactory;

		ContextScopeServiceComponent IUnrestrictedContextServicesProvider.ContextScope => ContextScope;

		IDiagnosticsService IGlobalContextStatefulServicesProvider.Diagnostics => StatefulServicesDiagnostics;

		IMessageLogger IGlobalContextStatefulServicesProvider.MessageLogger => StatefulServicesMessageLogger;

		IPathFactoryService IGlobalContextServicesProvider.PathFactory => ServicesPathFactory;

		IContextScopeService IGlobalContextServicesProvider.ContextScope => ContextScope;

		StatsComponent IUnrestrictedContextCollectorProvider.Stats => Stats;

		VTableBuilder IUnrestrictedContextCollectorProvider.VTableBuilder => VTableBuilder;

		CppDeclarationsComponent IUnrestrictedContextCollectorProvider.CppDeclarations => CppDeclarations;

		MatchedAssemblyMethodSourceFilesComponent IUnrestrictedContextCollectorProvider.MatchedAssemblyMethodSourceFiles => MatchedAssemblyMethodSourceFiles;

		ReversePInvokeWrapperComponent IUnrestrictedContextCollectorProvider.ReversePInvokeWrappers => ReversePInvokeWrappers;

		WindowsRuntimeTypeWithNameComponent IUnrestrictedContextCollectorProvider.WindowsRuntimeTypeWithNames => WindowsRuntimeTypeWithNames;

		CCWMarshallingFunctionComponent IUnrestrictedContextCollectorProvider.CCWMarshallingFunctions => CcwMarshallingFunctions;

		InteropGuidComponent IUnrestrictedContextCollectorProvider.InteropGuids => InteropGuids;

		TypeMarshallingFunctionsComponent IUnrestrictedContextCollectorProvider.TypeMarshallingFunctions => TypeMarshallingFunctions;

		WrapperForDelegateFromManagedToNativeComponent IUnrestrictedContextCollectorProvider.WrappersForDelegateFromManagedToNative => WrappersForDelegateFromManagedToNative;

		IInteropGuidCollector IGlobalContextCollectorProvider.InteropGuids => CollectorInteropGuids;

		ITypeMarshallingFunctionsCollector IGlobalContextCollectorProvider.TypeMarshallingFunctions => CollectorTypeMarshallingFunctions;

		IWrapperForDelegateFromManagedToNativeCollector IGlobalContextCollectorProvider.WrappersForDelegateFromManagedToNative => CollectorWrappersForDelegateFromManagedToNative;

		ICCWMarshallingFunctionCollector IGlobalContextCollectorProvider.CCWMarshallingFunctions => CollectorCcwMarshallingFunctions;

		IReversePInvokeWrapperCollector IGlobalContextCollectorProvider.ReversePInvokeWrappers => CollectorReversePInvokeWrappers;

		IWindowsRuntimeTypeWithNameCollector IGlobalContextCollectorProvider.WindowsRuntimeTypeWithNames => CollectorWindowsRuntimeTypeWithNames;

		IMethodCollector IGlobalContextCollectorProvider.Methods => CollectorMethods;

		ISharedMethodCollector IGlobalContextCollectorProvider.SharedMethods => CollectorSharedMethods;

		IVirtualCallCollector IGlobalContextCollectorProvider.VirtualCalls => CollectorVirtualCalls;

		ISymbolsCollector IGlobalContextCollectorProvider.Symbols => CollectorSymbols;

		ICppDeclarationsCacheWriter IGlobalContextCollectorProvider.CppDeclarationsCache => CollectorCppDeclarationsCache;

		ICppIncludeDepthCalculatorCache IGlobalContextCollectorProvider.CppIncludeDepthCalculatorCache => CollectorIncludeDepthCalculatorCache;

		IMatchedAssemblyMethodSourceFilesCollector IGlobalContextCollectorProvider.MatchedAssemblyMethodSourceFiles => CollectorMatchedAssemblyMethodSourceFiles;

		IAssemblyDependencyResults IGlobalContextServicesProvider.AssemblyDependencies => ServicesAssemblyDependencies;

		ICppDeclarationsCache IGlobalContextResultsProvider.CppDeclarationsCache => ResultCppDeclarationsCache;

		IMetadataUsageCollectorWriterService IGlobalContextCollectorProvider.MetadataUsage => CollectorMetadataUsage;

		IStatsWriterService IGlobalContextCollectorProvider.Stats => CollectorStats;

		ITypeCollector IGlobalContextCollectorProvider.Types => CollectorTypes;

		IGenericMethodCollector IGlobalContextCollectorProvider.GenericMethods => CollectorGenericMethods;

		IRuntimeImplementedMethodWriterCollector IGlobalContextCollectorProvider.RuntimeImplementedMethodWriters => CollectorRuntimeImplementedMethodWriter;

		IVTableBuilder IGlobalContextCollectorProvider.VTable => CollectorVTable;

		ITinyTypeCollector IGlobalContextCollectorProvider.TinyTypes => CollectorTinyTypes;

		ITinyStringCollector IGlobalContextCollectorProvider.TinyStrings => CollectorTinyStrings;

		ICallMappingComponent IUnrestrictedContextServicesProvider.ICallMapping => ICallMapping;

		GuidProviderComponent IUnrestrictedContextServicesProvider.GuidProvider => GuidProvider;

		TypeProviderComponent IUnrestrictedContextServicesProvider.TypeProvider => TypeProvider;

		AssemblyLoaderComponent IUnrestrictedContextServicesProvider.AssemblyLoader
		{
			get
			{
				throw new NotSupportedException("Trying to access an object that is only available directly from AssemblyConversionContext");
			}
		}

		WindowsRuntimeProjectionsComponent IUnrestrictedContextServicesProvider.WindowsRuntimeProjections => WindowsRuntimeProjections;

		ObjectFactoryComponent IUnrestrictedContextServicesProvider.Factory => Factory;

		DiagnosticsComponent IUnrestrictedContextStatefulServicesProvider.Diagnostics => Diagnostics;

		MessageLoggerComponent IUnrestrictedContextStatefulServicesProvider.MessageLogger => MessageLogger;

		IGuidProvider IGlobalContextServicesProvider.GuidProvider => ServicesGuidProvider;

		ITypeProviderService IGlobalContextServicesProvider.TypeProvider => ServicesTypeProvider;

		IObjectFactory IGlobalContextServicesProvider.Factory => ServicesFactory;

		IWindowsRuntimeProjections IGlobalContextServicesProvider.WindowsRuntime => ServicesWindowsRuntime;

		IICallMappingService IGlobalContextServicesProvider.ICallMapping => ServicesICallMapping;

		SourceAnnotationWriterComponent IUnrestrictedContextStatefulServicesProvider.SourceAnnotationWriter => SourceAnnotationWriter;

		NamingComponent IUnrestrictedContextStatefulServicesProvider.Naming => Naming;

		ErrorInformation IUnrestrictedContextStatefulServicesProvider.ErrorInformation => ErrorInformation;

		ImmediateSchedulerComponent IUnrestrictedContextStatefulServicesProvider.Scheduler => Scheduler;

		IWorkScheduler IGlobalContextStatefulServicesProvider.Scheduler => StatefulServicesScheduler;

		INamingService IGlobalContextStatefulServicesProvider.Naming => StatefulServicesNaming;

		ISourceAnnotationWriter IGlobalContextStatefulServicesProvider.SourceAnnotationWriter => StatefulServicesSourceAnnotationWriter;

		IErrorInformationService IGlobalContextStatefulServicesProvider.ErrorInformation => StatefulServicesErrorInformation;
	}
}
