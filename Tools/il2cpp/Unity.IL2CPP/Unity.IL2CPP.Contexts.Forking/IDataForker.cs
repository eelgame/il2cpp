using System;
using Unity.IL2CPP.Contexts.Components;
using Unity.IL2CPP.Metadata;
using Unity.IL2CPP.Naming;
using Unity.IL2CPP.Symbols;
using Unity.IL2CPP.Tiny;

namespace Unity.IL2CPP.Contexts.Forking
{
	public interface IDataForker<TContext>
	{
		TContext CreateForkedContext();

		Action ForkMethods(MethodCollector component);

		Action ForkSharedMethods(SharedMethodCollector component);

		Action ForkVirtualCalls(VirtualCallCollector component);

		Action ForkSymbols(SymbolsCollector component);

		Action ForkAssemblyDependencies(AssemblyDependenciesComponent component);

		Action ForkMetadataUsage(MetadataUsageCollectorComponent component);

		Action ForkStats(StatsComponent component);

		Action ForkTypes(TypeCollector component);

		Action ForkGenericMethods(GenericMethodCollectorComponent component);

		Action ForkRuntimeImplementedMethodWriters(RuntimeImplementedMethodWriterCollectorComponent component);

		Action ForkVTable(VTableBuilder component);

		Action ForkGuidProvider(GuidProviderComponent component);

		Action ForkTypeProvider(TypeProviderComponent component);

		Action ForkFactory(ObjectFactoryComponent component);

		Action ForkWindowsRuntime(WindowsRuntimeProjectionsComponent component);

		Action ForkICallMapping(ICallMappingComponent component);

		Action ForkNaming(NamingComponent component);

		Action ForkSourceAnnotationWriter(SourceAnnotationWriterComponent component);

		Action ForkErrorInformation(ErrorInformation component);

		Action ForkTinyStringCollector(TinyStringCollectorComponent component);

		Action ForkTinyTypeCollector(TinyTypeCollectorComponent component);

		Action ForkCppDeclarations(CppDeclarationsComponent component);

		Action ForkWorkers(ImmediateSchedulerComponent component);

		Action ForkMatchedAssemblyMethodSourceFiles(MatchedAssemblyMethodSourceFilesComponent component);

		Action ForkReversePInvokeWrappers(ReversePInvokeWrapperComponent component);

		Action ForkWindowsRuntimeTypeWithNames(WindowsRuntimeTypeWithNameComponent component);

		Action ForkCCWritingFunctions(CCWMarshallingFunctionComponent component);

		Action ForkInteropGuids(InteropGuidComponent component);

		Action ForkTypeMarshallingFunctions(TypeMarshallingFunctionsComponent component);

		Action ForkWrappersForDelegateFromManagedToNative(WrapperForDelegateFromManagedToNativeComponent component);

		Action ForkPathFactory(PathFactoryComponent component);

		Action ForkContextScopeService(ContextScopeServiceComponent component);

		Action ForkDiagnostics(DiagnosticsComponent component);

		Action ForkMessageLogger(MessageLoggerComponent component);
	}
}
