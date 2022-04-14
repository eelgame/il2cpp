using System;
using Unity.IL2CPP.Contexts.Components;
using Unity.IL2CPP.Contexts.Forking.Providers;
using Unity.IL2CPP.Metadata;
using Unity.IL2CPP.Naming;
using Unity.IL2CPP.Symbols;
using Unity.IL2CPP.Tiny;

namespace Unity.IL2CPP.Contexts.Forking.Steps
{
	public abstract class BaseDataForker<TContext, TLateAccess> : IDataForker<TContext> where TLateAccess : LateAccessForkingContainer
	{
		protected delegate void ReadWrite<TLateAccessDel, TWrite, TRead, TFull>(TLateAccessDel lateAccess, out TWrite write, out TRead read, out TFull full);

		protected delegate void ReadOnly<TLateAccessDel, TRead, TFull>(TLateAccessDel lateAccess, out object write, out TRead read, out TFull full);

		protected delegate void WriteOnly<TLateAccessDel, TWrite, TFull>(TLateAccessDel lateAccess, out TWrite write, out object read, out TFull full);

		private TLateAccess _lateAccess;

		protected readonly ForkedDataContainer _container;

		protected readonly ForkedDataProvider _forkedProvider;

		protected TLateAccess LateAccess
		{
			get
			{
				if (_lateAccess == null)
				{
					_lateAccess = CreateLateAccess();
				}
				return _lateAccess;
			}
		}

		protected BaseDataForker(IUnrestrictedContextDataProvider context)
			: this(new ForkedDataProvider(context, new ForkedDataContainer()))
		{
		}

		protected BaseDataForker(ForkedDataProvider forkedProvider)
		{
			_container = forkedProvider.Container;
			_forkedProvider = forkedProvider;
		}

		protected abstract TLateAccess CreateLateAccess();

		public abstract TContext CreateForkedContext();

		private Action ForkAndMergeReadWrite<TWrite, TRead, TFull>(ReadWrite<TLateAccess, TWrite, TRead, TFull> forkable, Action<TFull> merge, out TWrite writer, out TRead reader, out TFull full)
		{
			forkable(LateAccess, out writer, out reader, out full);
			if (writer == null)
			{
				throw new ArgumentNullException(string.Format("{0} returned a null `{1}` which is not allowed", forkable.GetType(), "writer"));
			}
			if (reader == null)
			{
				throw new ArgumentNullException(string.Format("{0} returned a null `{1}` which is not allowed", forkable.GetType(), "reader"));
			}
			if (full == null)
			{
				throw new ArgumentNullException(string.Format("{0} returned a null `{1}` which is not allowed", forkable.GetType(), "full"));
			}
			TFull tmpFull = full;
			return delegate
			{
				merge(tmpFull);
			};
		}

		private Action ForkAndMergeReadOnly<TRead, TFull>(ReadOnly<TLateAccess, TRead, TFull> forkable, Action<TFull> merge, out TRead reader, out TFull full)
		{
			forkable(LateAccess, out var _, out reader, out full);
			if (reader == null)
			{
				throw new ArgumentNullException(string.Format("{0} returned a null `{1}` which is not allowed", forkable.GetType(), "reader"));
			}
			if (full == null)
			{
				throw new ArgumentNullException(string.Format("{0} returned a null `{1}` which is not allowed", forkable.GetType(), "full"));
			}
			TFull tmpFull = full;
			return delegate
			{
				merge(tmpFull);
			};
		}

		private Action ForkAndMergeWriteOnly<TWrite, TFull>(WriteOnly<TLateAccess, TWrite, TFull> forkable, Action<TFull> merge, out TWrite writer, out TFull full)
		{
			forkable(_lateAccess, out writer, out var _, out full);
			if (writer == null)
			{
				throw new ArgumentNullException(string.Format("{0} returned a null `{1}` which is not allowed", forkable.GetType(), "writer"));
			}
			if (full == null)
			{
				throw new ArgumentNullException(string.Format("{0} returned a null `{1}` which is not allowed", forkable.GetType(), "full"));
			}
			TFull tmpFull = full;
			return delegate
			{
				merge(tmpFull);
			};
		}

		protected abstract ReadWrite<TLateAccess, TWrite, TRead, TFull> PickFork<TWrite, TRead, TFull>(IForkableComponent<TWrite, TRead, TFull> component);

		protected abstract WriteOnly<TLateAccess, TWrite, TFull> PickFork<TWrite, TFull>(IForkableComponent<TWrite, object, TFull> component);

		protected abstract ReadOnly<TLateAccess, TRead, TFull> PickFork<TRead, TFull>(IForkableComponent<object, TRead, TFull> component);

		protected abstract Action<TFull> PickMerge<TWrite, TRead, TFull>(IForkableComponent<TWrite, TRead, TFull> component);

		public Action ForkMethods(MethodCollector component)
		{
			return ForkAndMergeWriteOnly(PickFork(component), PickMerge(component), out _container.CollectorMethods, out _container.Methods);
		}

		public Action ForkSharedMethods(SharedMethodCollector component)
		{
			return ForkAndMergeWriteOnly(PickFork(component), PickMerge(component), out _container.CollectorSharedMethods, out _container.SharedMethods);
		}

		public Action ForkVirtualCalls(VirtualCallCollector component)
		{
			return ForkAndMergeWriteOnly(PickFork(component), PickMerge(component), out _container.CollectorVirtualCalls, out _container.VirtualCalls);
		}

		public Action ForkSymbols(SymbolsCollector component)
		{
			return ForkAndMergeWriteOnly(PickFork(component), PickMerge(component), out _container.CollectorSymbols, out _container.Symbols);
		}

		public Action ForkAssemblyDependencies(AssemblyDependenciesComponent component)
		{
			return ForkAndMergeReadOnly(PickFork(component), PickMerge(component), out _container.ServicesAssemblyDependencies, out _container.AssemblyDependencies);
		}

		public Action ForkMetadataUsage(MetadataUsageCollectorComponent component)
		{
			return ForkAndMergeWriteOnly(PickFork(component), PickMerge(component), out _container.CollectorMetadataUsage, out _container.MetadataUsage);
		}

		public Action ForkStats(StatsComponent component)
		{
			return ForkAndMergeWriteOnly(PickFork(component), PickMerge(component), out _container.CollectorStats, out _container.Stats);
		}

		public Action ForkTypes(TypeCollector component)
		{
			return ForkAndMergeWriteOnly(PickFork(component), PickMerge(component), out _container.CollectorTypes, out _container.TypeCollector);
		}

		public Action ForkGenericMethods(GenericMethodCollectorComponent component)
		{
			return ForkAndMergeWriteOnly(PickFork(component), PickMerge(component), out _container.CollectorGenericMethods, out _container.GenericMethodCollector);
		}

		public Action ForkRuntimeImplementedMethodWriters(RuntimeImplementedMethodWriterCollectorComponent component)
		{
			return ForkAndMergeWriteOnly(PickFork(component), PickMerge(component), out _container.CollectorRuntimeImplementedMethodWriter, out _container.RuntimeImplementedMethodWriterCollector);
		}

		public Action ForkVTable(VTableBuilder component)
		{
			return ForkAndMergeWriteOnly(PickFork(component), PickMerge(component), out _container.CollectorVTable, out _container.VTableBuilder);
		}

		public Action ForkGuidProvider(GuidProviderComponent component)
		{
			return ForkAndMergeReadOnly(PickFork(component), PickMerge(component), out _container.ServicesGuidProvider, out _container.GuidProvider);
		}

		public Action ForkTypeProvider(TypeProviderComponent component)
		{
			return ForkAndMergeReadOnly(PickFork(component), PickMerge(component), out _container.ServicesTypeProvider, out _container.TypeProvider);
		}

		public Action ForkFactory(ObjectFactoryComponent component)
		{
			return ForkAndMergeReadOnly(PickFork(component), PickMerge(component), out _container.ServicesFactory, out _container.Factory);
		}

		public Action ForkWindowsRuntime(WindowsRuntimeProjectionsComponent component)
		{
			return ForkAndMergeReadOnly(PickFork(component), PickMerge(component), out _container.ServicesWindowsRuntime, out _container.WindowsRuntimeProjections);
		}

		public Action ForkICallMapping(ICallMappingComponent component)
		{
			return ForkAndMergeReadOnly(PickFork(component), PickMerge(component), out _container.ServicesICallMapping, out _container.ICallMapping);
		}

		public Action ForkNaming(NamingComponent component)
		{
			return ForkAndMergeReadOnly(PickFork(component), PickMerge(component), out _container.StatefulServicesNaming, out _container.Naming);
		}

		public Action ForkSourceAnnotationWriter(SourceAnnotationWriterComponent component)
		{
			return ForkAndMergeWriteOnly(PickFork(component), PickMerge(component), out _container.StatefulServicesSourceAnnotationWriter, out _container.SourceAnnotationWriter);
		}

		public Action ForkErrorInformation(ErrorInformation component)
		{
			return ForkAndMergeWriteOnly(PickFork(component), PickMerge(component), out _container.StatefulServicesErrorInformation, out _container.ErrorInformation);
		}

		public Action ForkTinyStringCollector(TinyStringCollectorComponent component)
		{
			return ForkAndMergeWriteOnly(PickFork(component), PickMerge(component), out _container.CollectorTinyStrings, out _container.TinyStringCollector);
		}

		public Action ForkTinyTypeCollector(TinyTypeCollectorComponent component)
		{
			return ForkAndMergeWriteOnly(PickFork(component), PickMerge(component), out _container.CollectorTinyTypes, out _container.TinyTypeCollector);
		}

		public Action ForkCppDeclarations(CppDeclarationsComponent component)
		{
			Action result = ForkAndMergeReadWrite(PickFork(component), PickMerge(component), out _container.CollectorCppDeclarationsCache, out _container.ResultCppDeclarationsCache, out _container.CppDeclarations);
			_container.CollectorIncludeDepthCalculatorCache = _container.CollectorCppDeclarationsCache;
			return result;
		}

		public Action ForkWorkers(ImmediateSchedulerComponent component)
		{
			return ForkAndMergeWriteOnly(PickFork(component), PickMerge(component), out _container.StatefulServicesScheduler, out _container.Scheduler);
		}

		public Action ForkMatchedAssemblyMethodSourceFiles(MatchedAssemblyMethodSourceFilesComponent component)
		{
			return ForkAndMergeWriteOnly(PickFork(component), PickMerge(component), out _container.CollectorMatchedAssemblyMethodSourceFiles, out _container.MatchedAssemblyMethodSourceFiles);
		}

		public Action ForkReversePInvokeWrappers(ReversePInvokeWrapperComponent component)
		{
			return ForkAndMergeWriteOnly(PickFork(component), PickMerge(component), out _container.CollectorReversePInvokeWrappers, out _container.ReversePInvokeWrappers);
		}

		public Action ForkWindowsRuntimeTypeWithNames(WindowsRuntimeTypeWithNameComponent component)
		{
			return ForkAndMergeWriteOnly(PickFork(component), PickMerge(component), out _container.CollectorWindowsRuntimeTypeWithNames, out _container.WindowsRuntimeTypeWithNames);
		}

		public Action ForkCCWritingFunctions(CCWMarshallingFunctionComponent component)
		{
			return ForkAndMergeWriteOnly(PickFork(component), PickMerge(component), out _container.CollectorCcwMarshallingFunctions, out _container.CcwMarshallingFunctions);
		}

		public Action ForkInteropGuids(InteropGuidComponent component)
		{
			return ForkAndMergeWriteOnly(PickFork(component), PickMerge(component), out _container.CollectorInteropGuids, out _container.InteropGuids);
		}

		public Action ForkTypeMarshallingFunctions(TypeMarshallingFunctionsComponent component)
		{
			return ForkAndMergeWriteOnly(PickFork(component), PickMerge(component), out _container.CollectorTypeMarshallingFunctions, out _container.TypeMarshallingFunctions);
		}

		public Action ForkWrappersForDelegateFromManagedToNative(WrapperForDelegateFromManagedToNativeComponent component)
		{
			return ForkAndMergeWriteOnly(PickFork(component), PickMerge(component), out _container.CollectorWrappersForDelegateFromManagedToNative, out _container.WrappersForDelegateFromManagedToNative);
		}

		public Action ForkPathFactory(PathFactoryComponent component)
		{
			return ForkAndMergeReadOnly(PickFork(component), PickMerge(component), out _container.ServicesPathFactory, out _container.PathFactory);
		}

		public Action ForkContextScopeService(ContextScopeServiceComponent component)
		{
			return ForkAndMergeReadOnly(PickFork(component), PickMerge(component), out _container.ServicesContextScopeService, out _container.ContextScope);
		}

		public Action ForkDiagnostics(DiagnosticsComponent component)
		{
			return ForkAndMergeReadOnly(PickFork(component), PickMerge(component), out _container.StatefulServicesDiagnostics, out _container.Diagnostics);
		}

		public Action ForkMessageLogger(MessageLoggerComponent component)
		{
			return ForkAndMergeWriteOnly(PickFork(component), PickMerge(component), out _container.StatefulServicesMessageLogger, out _container.MessageLogger);
		}
	}
}
