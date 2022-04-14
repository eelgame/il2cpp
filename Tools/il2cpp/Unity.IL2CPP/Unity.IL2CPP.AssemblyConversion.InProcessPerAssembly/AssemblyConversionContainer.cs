using System;
using System.Collections.ObjectModel;
using System.Linq;
using Mono.Cecil;
using Unity.IL2CPP.AssemblyConversion.PrimaryCollection.Results;
using Unity.IL2CPP.AssemblyConversion.PrimaryCollection.Steps.PerAssembly;
using Unity.IL2CPP.AssemblyConversion.PrimaryWrite.Steps.Global;
using Unity.IL2CPP.AssemblyConversion.PrimaryWrite.Steps.PerAssembly;
using Unity.IL2CPP.AssemblyConversion.SecondaryCollection.Steps.Generics;
using Unity.IL2CPP.AssemblyConversion.SecondaryCollection.Steps.Global;
using Unity.IL2CPP.AssemblyConversion.SecondaryCollection.Steps.PerAssembly;
using Unity.IL2CPP.AssemblyConversion.SecondaryWrite.Steps.Global;
using Unity.IL2CPP.AssemblyConversion.SecondaryWrite.Steps.PerAssembly;
using Unity.IL2CPP.AssemblyConversion.Steps;
using Unity.IL2CPP.AssemblyConversion.Steps.Results;
using Unity.IL2CPP.Attributes;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Results;
using Unity.IL2CPP.Contexts.Results.Phases;
using Unity.IL2CPP.Contexts.Scheduling;
using Unity.IL2CPP.Debugger;
using Unity.IL2CPP.GenericsCollection;
using Unity.IL2CPP.Metadata;
using Unity.IL2CPP.StringLiterals;
using Unity.IL2CPP.Tiny;
using Unity.MiniProfiling;

namespace Unity.IL2CPP.AssemblyConversion.InProcessPerAssembly
{
	public class AssemblyConversionContainer : BaseConversionContainer
	{
		private readonly ReadOnlyCollection<AssemblyDefinition> _assemblies;

		private readonly bool _isEntryAssembly;

		public override string Name { get; }

		public override string CleanName { get; }

		public AssemblyConversionContainer(AssemblyDefinition definition, bool isEntryAssembly, string name, string cleanName, int index)
			: base(index)
		{
			_assemblies = new AssemblyDefinition[1] { definition }.AsReadOnly();
			_isEntryAssembly = isEntryAssembly;
			Name = name;
			CleanName = cleanName;
		}

		public override bool IncludeTypeDefinitionInContext(TypeReference type)
		{
			TypeReference nonPinnedAndNonByReferenceType = type.GetNonPinnedAndNonByReferenceType();
			if (nonPinnedAndNonByReferenceType.IsGenericParameter)
			{
				return ((GenericParameter)nonPinnedAndNonByReferenceType).Module.Assembly == _assemblies[0];
			}
			if (!(type is TypeSpecification))
			{
				return type.Resolve().Module.Assembly == _assemblies[0];
			}
			return true;
		}

		protected override AssemblyConversionResults.PrimaryCollectionPhase PrimaryCollectionPhase(GlobalFullyForkedContext context, GenericSharingAnalysisResults genericSharingAnalysisResults)
		{
			ReadOnlyPerAssemblyPendingResults<ReadOnlyCollectedAttributeSupportData> readOnlyPerAssemblyPendingResults;
			ReadOnlyPerAssemblyPendingResults<ISequencePointProvider> readOnlyPerAssemblyPendingResults2;
			ReadOnlyPerAssemblyPendingResults<ICatchPointProvider> readOnlyPerAssemblyPendingResults3;
			ReadOnlyPerAssemblyPendingResults<CollectedWindowsRuntimeData> readOnlyPerAssemblyPendingResults4;
			using (IPhaseWorkScheduler<GlobalPrimaryCollectionContext> scheduler = CreateHackedScheduler(context.GlobalPrimaryCollectionContext))
			{
				readOnlyPerAssemblyPendingResults = new AttributeSupportCollection().Schedule(scheduler, _assemblies);
				readOnlyPerAssemblyPendingResults2 = new SequencePointCollection().Schedule(scheduler, _assemblies);
				readOnlyPerAssemblyPendingResults3 = new CatchPointCollection().Schedule(scheduler, _assemblies);
				readOnlyPerAssemblyPendingResults4 = new WindowsRuntimeDataCollection().Schedule(scheduler, _assemblies);
				new CCWMarshalingFunctionCollection().Schedule(scheduler, _assemblies);
				new AssemblyCollection().Schedule(scheduler, _assemblies);
			}
			using (MiniProfiler.Section("Build Results"))
			{
				return new AssemblyConversionResults.PrimaryCollectionPhase(new SequencePointProviderCollection(readOnlyPerAssemblyPendingResults2.Result), new CatchPointCollectorCollection(readOnlyPerAssemblyPendingResults3.Result), ReadOnlyInflatedCollectionCollector.Empty, readOnlyPerAssemblyPendingResults.Result, context.Collectors.WindowsRuntimeTypeWithNames.Complete(), readOnlyPerAssemblyPendingResults4.Result, context.Collectors.CCWMarshallingFunctions.Complete(), genericSharingAnalysisResults);
			}
		}

		protected override AssemblyConversionResults.PrimaryWritePhase PrimaryWritePhase(GlobalFullyForkedContext context)
		{
			ReadOnlyPerAssemblyPendingResults<ReadOnlyAttributeWriterOutput> readOnlyPerAssemblyPendingResults;
			ReadOnlyPerAssemblyPendingResults<TinyPrimaryWriteResult> readOnlyPerAssemblyPendingResults2;
			using (IPhaseWorkScheduler<GlobalWriteContext> scheduler = CreateHackedScheduler(context.GlobalWriteContext))
			{
				using (context.Services.Diagnostics.BeginCollectorStateDump(context, "AfterWriteAssemblies"))
				{
					new WriteAssemblies().Schedule(scheduler, _assemblies);
				}
				using (context.Services.Diagnostics.BeginCollectorStateDump(context, "AfterWriteAttributes"))
				{
					readOnlyPerAssemblyPendingResults = new WriteAttributes().Schedule(scheduler, _assemblies);
				}
				using (context.Services.Diagnostics.BeginCollectorStateDump(context, "AfterWriteDriver"))
				{
					if (_isEntryAssembly)
					{
						new WriteExecutableDriver(_assemblies.Single()).Schedule(scheduler);
					}
				}
				using (context.Services.Diagnostics.BeginCollectorStateDump(context, "AfterWriteWindowsRuntimeFactories"))
				{
					new WriteWindowsRuntimeFactories().Schedule(scheduler);
				}
				using (context.Services.Diagnostics.BeginCollectorStateDump(context, "AfterWriteComCallableWrappers"))
				{
					new WriteComCallableWrappers().Schedule(scheduler);
				}
				using (context.Services.Diagnostics.BeginCollectorStateDump(context, "AfterWriteProjectedInterfacesByCCWs"))
				{
					new WriteProjectedInterfacesByComCallableWrappers().Schedule(scheduler);
				}
				using (context.StatefulServices.Diagnostics.BeginCollectorStateDump(context, "AfterWriteTinyPerAssemblyCode"))
				{
					readOnlyPerAssemblyPendingResults2 = new WriteTinyPerAssemblyCode().Schedule(scheduler, _assemblies);
				}
			}
			using (MiniProfiler.Section("Build Results"))
			{
				return new AssemblyConversionResults.PrimaryWritePhase(context.Collectors.SharedMethods.Complete(), context.Collectors.Methods.Complete(), readOnlyPerAssemblyPendingResults.Result, context.Collectors.ReversePInvokeWrappers.Complete(), context.Collectors.TypeMarshallingFunctions.Complete(), context.Collectors.WrappersForDelegateFromManagedToNative.Complete(), context.Collectors.InteropGuids.Complete(), context.Collectors.MetadataUsage.Complete(), readOnlyPerAssemblyPendingResults2.Result, null, context.Collectors.TinyTypeCollector.Complete(), context.Collectors.TinyStringCollector.Complete(), context.Collectors.GenericMethodCollector.Complete());
			}
		}

		protected override AssemblyConversionResults.SecondaryCollectionPhase SecondaryCollectionPhase(GlobalFullyForkedContext context)
		{
			ReadOnlyGlobalPendingResults<ReadOnlyInvokerCollection> readOnlyGlobalPendingResults = null;
			ReadOnlyGlobalPendingResults<ReadOnlyMethodTables> readOnlyGlobalPendingResults2 = null;
			ReadOnlyGlobalPendingResults<IMetadataCollectionResults> readOnlyGlobalPendingResults3 = null;
			ReadOnlyPerAssemblyPendingResults<GenericContextCollection> readOnlyPerAssemblyPendingResults = null;
			ITinyTypeMetadataResults tinyTypeMetadata = null;
			ITinyStringMetadataResults tinyStringMetadata = null;
			if (!context.GlobalSecondaryCollectionContext.Parameters.UsingTinyBackend)
			{
				using (IPhaseWorkScheduler<GlobalSecondaryCollectionContext> phaseWorkScheduler = CreateHackedScheduler(context.GlobalSecondaryCollectionContext))
				{
					using (context.StatefulServices.Diagnostics.BeginCollectorStateDump(context, "AfterCollectGenericContextMetadata"))
					{
						readOnlyPerAssemblyPendingResults = new CollectGenericContextMetadata().Schedule(phaseWorkScheduler, _assemblies);
					}
					using (context.StatefulServices.Diagnostics.BeginCollectorStateDump(context, "AfterCollectMetadata"))
					{
						readOnlyGlobalPendingResults3 = new CollectMetadata().Schedule(phaseWorkScheduler, _assemblies);
					}
					readOnlyGlobalPendingResults = new CollectInvokers().Schedule(phaseWorkScheduler, _assemblies, phaseWorkScheduler.ContextForMainThread.Results.PrimaryWrite.GenericMethods.UnsortedKeys);
					readOnlyGlobalPendingResults2 = new CollectMethodTables().Schedule(phaseWorkScheduler);
				}
			}
			else
			{
				tinyTypeMetadata = (context.GlobalReadOnlyContext.Parameters.UsingTinyBackend ? TinyTypeMetadataCollector.Collect(context.CreateReadOnlyContext(), context.Results.PrimaryWrite.TinyTypes) : null);
				tinyStringMetadata = (context.GlobalReadOnlyContext.Parameters.UsingTinyBackend ? TinyStringMetadataCollector.Collect(context.CreateReadOnlyContext(), context.Results.PrimaryWrite.TinyStrings) : null);
			}
			using (MiniProfiler.Section("Build Results"))
			{
				return new AssemblyConversionResults.SecondaryCollectionPhase(readOnlyGlobalPendingResults?.Result, readOnlyGlobalPendingResults2?.Result, readOnlyPerAssemblyPendingResults?.Result, readOnlyGlobalPendingResults3?.Result, context.Collectors.TypeCollector.Complete(), tinyTypeMetadata, tinyStringMetadata);
			}
		}

		protected override AssemblyConversionResults.SecondaryWritePhasePart1 SecondaryWritePhasePart1(GlobalFullyForkedContext context)
		{
			using (context.Services.Diagnostics.BeginCollectorStateDump(context, "AfterWriteDebuggerTables"))
			{
				SecondaryWriteSteps.WriteDebuggerTables(context.GlobalWriteContext, _assemblies);
			}
			using (MiniProfiler.Section("Build Results"))
			{
				return new AssemblyConversionResults.SecondaryWritePhasePart1(context.Collectors.VirtualCalls.Complete());
			}
		}

		protected override AssemblyConversionResults.SecondaryWritePhasePart3 SecondaryWritePhasePart3(GlobalFullyForkedContext context)
		{
			UnresolvedVirtualsTablesInfo virtualCallTables;
			using (context.Services.Diagnostics.BeginCollectorStateDump(context, "AfterWriteUnresolvedStubs"))
			{
				SecondaryWriteSteps.WriteUnresolvedVirtualCalls(context.GlobalWriteContext, out virtualCallTables);
			}
			using (MiniProfiler.Section("Build Results"))
			{
				return new AssemblyConversionResults.SecondaryWritePhasePart3(virtualCallTables);
			}
		}

		protected override AssemblyConversionResults.SecondaryWritePhase SecondaryWritePhasePart4(GlobalFullyForkedContext context)
		{
			ReadOnlyGlobalPendingResults<(IStringLiteralCollection, IFieldReferenceCollection)> readOnlyGlobalPendingResults = null;
			using (IPhaseWorkScheduler<GlobalWriteContext> scheduler = CreateHackedScheduler(context.GlobalWriteContext))
			{
				using (context.StatefulServices.Diagnostics.BeginCollectorStateDump(context, "AfterWriteCodeMetadata"))
				{
					new WriteFullPerAssemblyCodeMetadata(MetadataCacheWriter.RegistrationTableName(context.CreateReadOnlyContext()), CodeRegistrationWriter.CodeRegistrationTableName(context.CreateReadOnlyContext())).Schedule(scheduler, _assemblies);
				}
				using (context.StatefulServices.Diagnostics.BeginCollectorStateDump(context, "AfterWriteMetadata"))
				{
					readOnlyGlobalPendingResults = new WriteGlobalMetadata().Schedule(scheduler, _assemblies);
				}
				using (context.StatefulServices.Diagnostics.BeginCollectorStateDump(context, "AfterWriteCodeRegistration"))
				{
					new WriteFullPerAssemblyCodeRegistration().Schedule(scheduler);
				}
			}
			using (MiniProfiler.Section("Build Results"))
			{
				return new AssemblyConversionResults.SecondaryWritePhase(readOnlyGlobalPendingResults.Result.Item1, readOnlyGlobalPendingResults.Result.Item2, context.Collectors.Symbols.Complete());
			}
		}

		protected override void MetadataWritePhase(GlobalFullyForkedContext context)
		{
			throw new NotImplementedException();
		}

		protected override void CompletionPhase(GlobalFullyForkedContext context)
		{
			using (context.StatefulServices.Diagnostics.BeginCollectorStateDump(context, "AfterEmitMethodMap"))
			{
				CompletionSteps.EmitMethodMap(context.GlobalWriteContext);
			}
			using (context.StatefulServices.Diagnostics.BeginCollectorStateDump(context, "AfterEmitLineMapping"))
			{
				CompletionSteps.EmitLineMappingFile(context.GlobalReadOnlyContext, context.Results.SecondaryWrite.Symbols, context.GlobalReadOnlyContext.InputData.SymbolsFolder);
			}
		}
	}
}
