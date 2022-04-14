using System;
using System.Collections.ObjectModel;
using Mono.Cecil;
using NiceIO;
using Unity.IL2CPP.AssemblyConversion;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts.Collectors;
using Unity.IL2CPP.Contexts.Components;
using Unity.IL2CPP.Contexts.Forking;
using Unity.IL2CPP.Contexts.Forking.Providers;
using Unity.IL2CPP.Contexts.Results;
using Unity.IL2CPP.Contexts.Services;

namespace Unity.IL2CPP.Contexts
{
	public class GlobalWriteContext
	{
		public class ContextCollectors
		{
			public readonly IMethodCollector Methods;

			public readonly ISharedMethodCollector SharedMethods;

			public readonly IVirtualCallCollector VirtualCalls;

			public readonly ISymbolsCollector Symbols;

			public readonly IMetadataUsageCollectorWriterService MetadataUsage;

			public readonly IStatsWriterService Stats;

			public readonly ITypeCollector Types;

			public readonly IGenericMethodCollector GenericMethods;

			public readonly IVTableBuilder VTable;

			public readonly ICppDeclarationsCacheWriter CppDeclarationsCache;

			public readonly ICppIncludeDepthCalculatorCache CppIncludeDepthCalculatorCache;

			public readonly IMatchedAssemblyMethodSourceFilesCollector MatchedAssemblyMethodSourceFiles;

			public readonly IReversePInvokeWrapperCollector ReversePInvokeWrappers;

			public readonly IInteropGuidCollector InteropGuids;

			public readonly ITypeMarshallingFunctionsCollector TypeMarshallingFunctions;

			public readonly IWrapperForDelegateFromManagedToNativeCollector WrappersForDelegateFromManagedToNative;

			public ContextCollectors(IGlobalContextCollectorProvider collectorProvider)
			{
				Methods = collectorProvider.Methods;
				SharedMethods = collectorProvider.SharedMethods;
				VirtualCalls = collectorProvider.VirtualCalls;
				Symbols = collectorProvider.Symbols;
				MetadataUsage = collectorProvider.MetadataUsage;
				Stats = collectorProvider.Stats;
				Types = collectorProvider.Types;
				GenericMethods = collectorProvider.GenericMethods;
				VTable = collectorProvider.VTable;
				CppDeclarationsCache = collectorProvider.CppDeclarationsCache;
				CppIncludeDepthCalculatorCache = collectorProvider.CppIncludeDepthCalculatorCache;
				MatchedAssemblyMethodSourceFiles = collectorProvider.MatchedAssemblyMethodSourceFiles;
				ReversePInvokeWrappers = collectorProvider.ReversePInvokeWrappers;
				InteropGuids = collectorProvider.InteropGuids;
				TypeMarshallingFunctions = collectorProvider.TypeMarshallingFunctions;
				WrappersForDelegateFromManagedToNative = collectorProvider.WrappersForDelegateFromManagedToNative;
			}
		}

		public class ContextResults
		{
			private readonly IGlobalContextPhaseResultsProvider _phaseResults;

			public readonly ICppDeclarationsCache CppDeclarationsCache;

			public AssemblyConversionResults.PrimaryCollectionPhase PrimaryCollection => _phaseResults.PrimaryCollection;

			public AssemblyConversionResults.SecondaryCollectionPhase SecondaryCollection => _phaseResults.SecondaryCollection;

			public AssemblyConversionResults.PrimaryWritePhase PrimaryWrite => _phaseResults.PrimaryWrite;

			public AssemblyConversionResults.SecondaryWritePhasePart1 SecondaryWritePart1 => _phaseResults.SecondaryWritePart1;

			public AssemblyConversionResults.SecondaryWritePhasePart3 SecondaryWritePart3 => _phaseResults.SecondaryWritePart3;

			public AssemblyConversionResults.SetupPhase Setup => _phaseResults.Setup;

			public ContextResults(IGlobalContextPhaseResultsProvider phaseResults, IGlobalContextResultsProvider resultsProvider)
			{
				_phaseResults = phaseResults;
				CppDeclarationsCache = resultsProvider.CppDeclarationsCache;
			}
		}

		public class ContextServices
		{
			public readonly INamingService Naming;

			public readonly ITypeProviderService TypeProvider;

			public readonly IObjectFactory Factory;

			public readonly IWindowsRuntimeProjections WindowsRuntime;

			public readonly IICallMappingService ICallMapping;

			internal readonly ISourceAnnotationWriter SourceAnnotationWriter;

			public readonly IErrorInformationService ErrorInformation;

			public readonly IWorkScheduler Scheduler;

			public readonly IPathFactoryService PathFactory;

			public readonly IMessageLogger MessageLogger;

			public ContextServices(IGlobalContextServicesProvider servicesProvider, IGlobalContextStatefulServicesProvider statefulServicesProvider)
			{
				Naming = statefulServicesProvider.Naming;
				TypeProvider = servicesProvider.TypeProvider;
				Factory = servicesProvider.Factory;
				WindowsRuntime = servicesProvider.WindowsRuntime;
				ICallMapping = servicesProvider.ICallMapping;
				SourceAnnotationWriter = statefulServicesProvider.SourceAnnotationWriter;
				ErrorInformation = statefulServicesProvider.ErrorInformation;
				Scheduler = statefulServicesProvider.Scheduler;
				PathFactory = servicesProvider.PathFactory;
				MessageLogger = statefulServicesProvider.MessageLogger;
			}
		}

		private readonly IUnrestrictedContextDataProvider _parent;

		private readonly GlobalMinimalContext _globalMinimalContext;

		private readonly GlobalReadOnlyContext _globalReadOnlyContext;

		public readonly ContextCollectors Collectors;

		public readonly ContextServices Services;

		public readonly ContextResults Results;

		public readonly AssemblyConversionInputData InputData;

		public readonly AssemblyConversionParameters Parameters;

		public AssemblyConversionResults.PrimaryCollectionPhase PrimaryCollectionResults => Results.PrimaryCollection;

		public AssemblyConversionResults.SecondaryCollectionPhase SecondaryCollectionResults => Results.SecondaryCollection;

		public AssemblyConversionResults.PrimaryWritePhase PrimaryWriteResults => Results.PrimaryWrite;

		public GlobalWriteContext(AssemblyConversionContext assemblyConversionContext)
			: this(assemblyConversionContext.ContextDataProvider, assemblyConversionContext.ContextDataProvider, assemblyConversionContext.GlobalMinimalContext, assemblyConversionContext.GlobalReadOnlyContext)
		{
		}

		public GlobalWriteContext(IUnrestrictedContextDataProvider parent, IGlobalContextDataProvider provider, GlobalMinimalContext globalMinimalContext, GlobalReadOnlyContext globalReadOnlyContext)
		{
			if (parent == null)
			{
				throw new ArgumentNullException("parent");
			}
			if (provider == null)
			{
				throw new ArgumentNullException("provider");
			}
			if (globalReadOnlyContext == null)
			{
				throw new ArgumentNullException("globalReadOnlyContext");
			}
			if (globalMinimalContext == null)
			{
				throw new ArgumentNullException("globalMinimalContext");
			}
			_parent = parent;
			_globalMinimalContext = globalMinimalContext;
			_globalReadOnlyContext = globalReadOnlyContext;
			Collectors = new ContextCollectors(provider.Collectors);
			Services = new ContextServices(provider.Services, provider.StatefulServices);
			Results = new ContextResults(provider.PhaseResults, provider.Results);
			Parameters = provider.Parameters;
			InputData = provider.InputData;
		}

		public ForkedContextScope<TItem, GlobalWriteContext> ForkForPrimaryWrite<TItem>(TItem[] items)
		{
			return ContextForker.ForPrimaryWrite(_parent, items);
		}

		public ForkedContextScope<TItem, GlobalWriteContext> ForkForPrimaryWrite<TItem>(ReadOnlyCollection<TItem> items)
		{
			return ContextForker.ForPrimaryWrite(_parent, items);
		}

		public ForkedContextScope<int, GlobalWriteContext> ForkForPrimaryWrite(int count)
		{
			return ContextForker.ForPrimaryWrite(_parent, count);
		}

		public ForkedContextScope<TItem, GlobalWriteContext> ForkFor<TItem>(Func<IUnrestrictedContextDataProvider, ForkedContextScope<TItem, GlobalWriteContext>> forker)
		{
			return forker(_parent);
		}

		public GlobalMinimalContext AsMinimal()
		{
			return _globalMinimalContext;
		}

		public GlobalReadOnlyContext AsReadOnly()
		{
			return _globalReadOnlyContext;
		}

		public MinimalContext CreateMinimalContext()
		{
			return AsMinimal().CreateMinimalContext();
		}

		public ReadOnlyContext GetReadOnlyContext()
		{
			return AsReadOnly().GetReadOnlyContext();
		}

		public SourceWritingContext CreateSourceWritingContext()
		{
			return new SourceWritingContext(this);
		}

		public ICppCodeWriter CreateProfiledSourceWriterInOutputDirectory(NPath filename)
		{
			return CreateSourceWritingContext().CreateProfiledSourceWriterInOutputDirectory(filename);
		}

		public IGeneratedMethodCodeWriter CreateProfiledSourceWriter(NPath filename)
		{
			return CreateSourceWritingContext().CreateProfiledManagedSourceWriter(filename);
		}

		public AssemblyWriteContext CreateAssemblyWritingContext(AssemblyDefinition assembly)
		{
			return new AssemblyWriteContext(CreateSourceWritingContext(), assembly);
		}
	}
}
