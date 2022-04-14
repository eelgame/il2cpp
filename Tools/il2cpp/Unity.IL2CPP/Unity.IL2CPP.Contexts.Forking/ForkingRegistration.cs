using System;
using System.Collections.ObjectModel;
using Unity.IL2CPP.Contexts.Forking.Providers;

namespace Unity.IL2CPP.Contexts.Forking
{
	public static class ForkingRegistration
	{
		public static void SetupMergeEntries<TContext>(IUnrestrictedContextDataProvider context, Action<object, Func<IDataForker<TContext>, int, Action>> registerCollector, ReadOnlyCollection<OverrideObjects> overrideObjects)
		{
			registerCollector(context.Collectors.MetadataUsage, (IDataForker<TContext> provider, int index) => provider.ForkMetadataUsage(context.Collectors.MetadataUsage));
			registerCollector(context.Collectors.Methods, (IDataForker<TContext> provider, int index) => provider.ForkMethods(context.Collectors.Methods));
			registerCollector(context.Collectors.Symbols, (IDataForker<TContext> provider, int index) => provider.ForkSymbols(context.Collectors.Symbols));
			registerCollector(context.Collectors.VirtualCalls, (IDataForker<TContext> provider, int index) => provider.ForkVirtualCalls(context.Collectors.VirtualCalls));
			registerCollector(context.Collectors.SharedMethods, (IDataForker<TContext> provider, int index) => provider.ForkSharedMethods(context.Collectors.SharedMethods));
			registerCollector(context.Collectors.TypeCollector, (IDataForker<TContext> provider, int index) => provider.ForkTypes(context.Collectors.TypeCollector));
			registerCollector(context.Collectors.GenericMethodCollector, (IDataForker<TContext> provider, int index) => provider.ForkGenericMethods(context.Collectors.GenericMethodCollector));
			registerCollector(context.Collectors.TinyStringCollector, (IDataForker<TContext> provider, int index) => provider.ForkTinyStringCollector(context.Collectors.TinyStringCollector));
			registerCollector(context.Collectors.TinyTypeCollector, (IDataForker<TContext> provider, int index) => provider.ForkTinyTypeCollector(context.Collectors.TinyTypeCollector));
			registerCollector(context.Collectors.RuntimeImplementedMethodWriterCollector, (IDataForker<TContext> provider, int index) => provider.ForkRuntimeImplementedMethodWriters(context.Collectors.RuntimeImplementedMethodWriterCollector));
			registerCollector(context.Collectors.Stats, (IDataForker<TContext> provider, int index) => provider.ForkStats(context.Collectors.Stats));
			registerCollector(context.Collectors.VTableBuilder, (IDataForker<TContext> provider, int index) => provider.ForkVTable(context.Collectors.VTableBuilder));
			registerCollector(context.Collectors.CppDeclarations, (IDataForker<TContext> provider, int index) => provider.ForkCppDeclarations(context.Collectors.CppDeclarations));
			registerCollector(context.Collectors.MatchedAssemblyMethodSourceFiles, (IDataForker<TContext> provider, int index) => provider.ForkMatchedAssemblyMethodSourceFiles(context.Collectors.MatchedAssemblyMethodSourceFiles));
			registerCollector(context.Collectors.ReversePInvokeWrappers, (IDataForker<TContext> provider, int index) => provider.ForkReversePInvokeWrappers(context.Collectors.ReversePInvokeWrappers));
			registerCollector(context.Collectors.WindowsRuntimeTypeWithNames, (IDataForker<TContext> provider, int index) => provider.ForkWindowsRuntimeTypeWithNames(context.Collectors.WindowsRuntimeTypeWithNames));
			registerCollector(context.Collectors.CCWMarshallingFunctions, (IDataForker<TContext> provider, int index) => provider.ForkCCWritingFunctions(context.Collectors.CCWMarshallingFunctions));
			registerCollector(context.Collectors.InteropGuids, (IDataForker<TContext> provider, int index) => provider.ForkInteropGuids(context.Collectors.InteropGuids));
			registerCollector(context.Collectors.TypeMarshallingFunctions, (IDataForker<TContext> provider, int index) => provider.ForkTypeMarshallingFunctions(context.Collectors.TypeMarshallingFunctions));
			registerCollector(context.Collectors.WrappersForDelegateFromManagedToNative, (IDataForker<TContext> provider, int index) => provider.ForkWrappersForDelegateFromManagedToNative(context.Collectors.WrappersForDelegateFromManagedToNative));
			registerCollector(context.Services.Factory, (IDataForker<TContext> provider, int index) => provider.ForkFactory(context.Services.Factory));
			registerCollector(context.Services.GuidProvider, (IDataForker<TContext> provider, int index) => provider.ForkGuidProvider(context.Services.GuidProvider));
			registerCollector(context.Services.TypeProvider, (IDataForker<TContext> provider, int index) => provider.ForkTypeProvider(context.Services.TypeProvider));
			registerCollector(context.Services.ICallMapping, (IDataForker<TContext> provider, int index) => provider.ForkICallMapping(context.Services.ICallMapping));
			registerCollector(context.Services.WindowsRuntimeProjections, (IDataForker<TContext> provider, int index) => provider.ForkWindowsRuntime(context.Services.WindowsRuntimeProjections));
			registerCollector(context.Services.AssemblyDependencies, (IDataForker<TContext> provider, int index) => provider.ForkAssemblyDependencies(context.Services.AssemblyDependencies));
			registerCollector(context.Services.PathFactory, (IDataForker<TContext> provider, int index) => provider.ForkPathFactory(Pick(context.Services.PathFactory, overrideObjects?[index]?.PathFactory)));
			registerCollector(context.Services.ContextScope, (IDataForker<TContext> provider, int index) => provider.ForkContextScopeService(Pick(context.Services.ContextScope, overrideObjects?[index]?.ContextScope)));
			registerCollector(context.StatefulServices.Naming, (IDataForker<TContext> provider, int index) => provider.ForkNaming(context.StatefulServices.Naming));
			registerCollector(context.StatefulServices.ErrorInformation, (IDataForker<TContext> provider, int index) => provider.ForkErrorInformation(context.StatefulServices.ErrorInformation));
			registerCollector(context.StatefulServices.SourceAnnotationWriter, (IDataForker<TContext> provider, int index) => provider.ForkSourceAnnotationWriter(context.StatefulServices.SourceAnnotationWriter));
			registerCollector(context.StatefulServices.Scheduler, (IDataForker<TContext> provider, int index) => provider.ForkWorkers(Pick(context.StatefulServices.Scheduler, overrideObjects?[index]?.Workers)));
			registerCollector(context.StatefulServices.Diagnostics, (IDataForker<TContext> provider, int index) => provider.ForkDiagnostics(context.StatefulServices.Diagnostics));
			registerCollector(context.StatefulServices.MessageLogger, (IDataForker<TContext> provider, int index) => provider.ForkMessageLogger(context.StatefulServices.MessageLogger));
		}

		private static T Pick<T>(T @default, T @override)
		{
			if (@override != null)
			{
				return @override;
			}
			return @default;
		}
	}
}
