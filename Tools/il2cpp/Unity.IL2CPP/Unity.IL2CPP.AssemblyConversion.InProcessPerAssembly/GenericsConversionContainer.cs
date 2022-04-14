using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Mono.Cecil;
using Unity.IL2CPP.AssemblyConversion.PrimaryCollection.Results;
using Unity.IL2CPP.AssemblyConversion.PrimaryCollection.Steps.Global;
using Unity.IL2CPP.AssemblyConversion.PrimaryWrite.Steps.Global;
using Unity.IL2CPP.AssemblyConversion.SecondaryCollection.Steps.Generics;
using Unity.IL2CPP.AssemblyConversion.SecondaryWrite.Steps.Generic;
using Unity.IL2CPP.AssemblyConversion.SecondaryWrite.Steps.Global;
using Unity.IL2CPP.AssemblyConversion.SecondaryWrite.Steps.PerAssembly;
using Unity.IL2CPP.AssemblyConversion.Steps;
using Unity.IL2CPP.AssemblyConversion.Steps.Results;
using Unity.IL2CPP.Attributes;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Results.Phases;
using Unity.IL2CPP.Contexts.Scheduling;
using Unity.IL2CPP.GenericsCollection;
using Unity.IL2CPP.Metadata;
using Unity.IL2CPP.StringLiterals;
using Unity.IL2CPP.Tiny;
using Unity.MiniProfiling;

namespace Unity.IL2CPP.AssemblyConversion.InProcessPerAssembly
{
	public class GenericsConversionContainer : BaseConversionContainer
	{
		public const string GenericsContainerCleanName = "Il2CppGenerics";

		private readonly ReadOnlyCollection<AssemblyDefinition> _allAssemblies;

		public override string Name => "Generics";

		public override string CleanName => "Il2CppGenerics";

		public GenericsConversionContainer(ReadOnlyCollection<AssemblyDefinition> allAssemblies, int index)
			: base(index)
		{
			_allAssemblies = allAssemblies;
		}

		public override bool IncludeTypeDefinitionInContext(TypeReference type)
		{
			if (type is TypeSpecification)
			{
				return !type.GetNonPinnedAndNonByReferenceType().IsGenericParameter;
			}
			return false;
		}

		protected override AssemblyConversionResults.PrimaryCollectionPhase PrimaryCollectionPhase(GlobalFullyForkedContext context, GenericSharingAnalysisResults genericSharingAnalysisResults)
		{
			ReadOnlyGlobalPendingResults<ReadOnlyInflatedCollectionCollector> readOnlyGlobalPendingResults;
			using (IPhaseWorkScheduler<GlobalPrimaryCollectionContext> scheduler = CreateHackedScheduler(context.GlobalPrimaryCollectionContext))
			{
				using (context.Services.Diagnostics.BeginCollectorStateDump(context, "AfterGenericsCollection"))
				{
					readOnlyGlobalPendingResults = new Unity.IL2CPP.AssemblyConversion.PrimaryCollection.Steps.Global.GenericsCollection(includeGenerics: true).Schedule(scheduler, _allAssemblies);
				}
			}
			using (MiniProfiler.Section("Build Results"))
			{
				return new AssemblyConversionResults.PrimaryCollectionPhase(SequencePointProviderCollection.Empty, CatchPointCollectorCollection.Empty, readOnlyGlobalPendingResults.Result, new Dictionary<AssemblyDefinition, ReadOnlyCollectedAttributeSupportData>().AsReadOnly(), context.Collectors.WindowsRuntimeTypeWithNames.Complete(), new Dictionary<AssemblyDefinition, CollectedWindowsRuntimeData>().AsReadOnly(), context.Collectors.CCWMarshallingFunctions.Complete(), genericSharingAnalysisResults);
			}
		}

		protected override AssemblyConversionResults.PrimaryWritePhase PrimaryWritePhase(GlobalFullyForkedContext context)
		{
			ReadOnlyGlobalPendingResults<TinyPrimaryWriteResult> readOnlyGlobalPendingResults;
			using (IPhaseWorkScheduler<GlobalWriteContext> scheduler = CreateHackedScheduler(context.GlobalWriteContext))
			{
				using (context.Services.Diagnostics.BeginCollectorStateDump(context, "AfterWriteGenerics"))
				{
					new WriteGenerics(includeGenerics: true).Schedule(scheduler);
				}
				using (context.Services.Diagnostics.BeginCollectorStateDump(context, "AfterWriteComCallableWrappers"))
				{
					new WriteComCallableWrappers().Schedule(scheduler);
				}
				using (context.Services.Diagnostics.BeginCollectorStateDump(context, "AfterWriteProjectedInterfacesByCCWs"))
				{
					new WriteProjectedInterfacesByComCallableWrappers().Schedule(scheduler);
				}
				using (context.StatefulServices.Diagnostics.BeginCollectorStateDump(context, "AfterWriteTinyGenericsCode"))
				{
					readOnlyGlobalPendingResults = new WriteTinyGenericCode().Schedule(scheduler);
				}
			}
			using (MiniProfiler.Section("Build Results"))
			{
				return new AssemblyConversionResults.PrimaryWritePhase(context.Collectors.SharedMethods.Complete(), context.Collectors.Methods.Complete(), new Dictionary<AssemblyDefinition, ReadOnlyAttributeWriterOutput>().AsReadOnly(), context.Collectors.ReversePInvokeWrappers.Complete(), context.Collectors.TypeMarshallingFunctions.Complete(), context.Collectors.WrappersForDelegateFromManagedToNative.Complete(), context.Collectors.InteropGuids.Complete(), context.Collectors.MetadataUsage.Complete(), new Dictionary<AssemblyDefinition, TinyPrimaryWriteResult>().AsReadOnly(), readOnlyGlobalPendingResults.Result, context.Collectors.TinyTypeCollector.Complete(), context.Collectors.TinyStringCollector.Complete(), context.Collectors.GenericMethodCollector.Complete());
			}
		}

		protected override AssemblyConversionResults.SecondaryCollectionPhase SecondaryCollectionPhase(GlobalFullyForkedContext context)
		{
			ReadOnlyGlobalPendingResults<ReadOnlyMethodTables> readOnlyGlobalPendingResults = null;
			ITinyTypeMetadataResults tinyTypeMetadata = null;
			ITinyStringMetadataResults tinyStringMetadata = null;
			if (!context.GlobalSecondaryCollectionContext.Parameters.UsingTinyBackend)
			{
				using (IPhaseWorkScheduler<GlobalSecondaryCollectionContext> scheduler = CreateHackedScheduler(context.GlobalSecondaryCollectionContext))
				{
					readOnlyGlobalPendingResults = new CollectMethodTables().Schedule(scheduler);
				}
			}
			else
			{
				tinyTypeMetadata = (context.GlobalReadOnlyContext.Parameters.UsingTinyBackend ? TinyTypeMetadataCollector.Collect(context.CreateReadOnlyContext(), context.Results.PrimaryWrite.TinyTypes) : null);
				tinyStringMetadata = (context.GlobalReadOnlyContext.Parameters.UsingTinyBackend ? TinyStringMetadataCollector.Collect(context.CreateReadOnlyContext(), context.Results.PrimaryWrite.TinyStrings) : null);
			}
			using (MiniProfiler.Section("Build Results"))
			{
				return new AssemblyConversionResults.SecondaryCollectionPhase(null, readOnlyGlobalPendingResults?.Result, new Dictionary<AssemblyDefinition, GenericContextCollection>().AsReadOnly(), new MetadataCollector(), context.Collectors.TypeCollector.Complete(), tinyTypeMetadata, tinyStringMetadata);
			}
		}

		protected override AssemblyConversionResults.SecondaryWritePhasePart1 SecondaryWritePhasePart1(GlobalFullyForkedContext context)
		{
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
				using (context.StatefulServices.Diagnostics.BeginCollectorStateDump(context, "AfterWriteMetadata"))
				{
					readOnlyGlobalPendingResults = new WriteGlobalMetadata().Schedule(scheduler, new AssemblyDefinition[0].AsReadOnly());
				}
				using (context.StatefulServices.Diagnostics.BeginCollectorStateDump(context, "AfterWriteCodeRegistration"))
				{
					new WriteFullPerAssemblyCodeRegistration().Schedule(scheduler);
				}
				new WriteGenericsPseudoCodeGenModule(CleanName).Schedule(scheduler);
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
