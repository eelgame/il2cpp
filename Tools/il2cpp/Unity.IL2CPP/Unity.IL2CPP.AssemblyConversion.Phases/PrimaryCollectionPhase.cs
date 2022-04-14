using System.Collections.ObjectModel;
using Mono.Cecil;
using Unity.IL2CPP.AssemblyConversion.PrimaryCollection.Results;
using Unity.IL2CPP.AssemblyConversion.PrimaryCollection.Steps.Global;
using Unity.IL2CPP.AssemblyConversion.PrimaryCollection.Steps.PerAssembly;
using Unity.IL2CPP.AssemblyConversion.Steps.Results;
using Unity.IL2CPP.Attributes;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Results.Phases;
using Unity.IL2CPP.Contexts.Scheduling;
using Unity.IL2CPP.Debugger;
using Unity.IL2CPP.GenericsCollection;
using Unity.MiniProfiling;

namespace Unity.IL2CPP.AssemblyConversion.Phases
{
	internal static class PrimaryCollectionPhase
	{
		public static void Run(AssemblyConversionContext context, ReadOnlyCollection<AssemblyDefinition> assemblies, bool includeGenerics = true)
		{
			using (MiniProfiler.Section("PrimaryCollectionPhase"))
			{
				ReadOnlyGlobalPendingResults<ReadOnlyInflatedCollectionCollector> readOnlyGlobalPendingResults;
				ReadOnlyGlobalPendingResults<GenericSharingAnalysisResults> readOnlyGlobalPendingResults2;
				ReadOnlyPerAssemblyPendingResults<ReadOnlyCollectedAttributeSupportData> readOnlyPerAssemblyPendingResults;
				ReadOnlyPerAssemblyPendingResults<ISequencePointProvider> readOnlyPerAssemblyPendingResults2;
				ReadOnlyPerAssemblyPendingResults<ICatchPointProvider> readOnlyPerAssemblyPendingResults3;
				ReadOnlyPerAssemblyPendingResults<CollectedWindowsRuntimeData> readOnlyPerAssemblyPendingResults4;
				using (IPhaseWorkScheduler<GlobalPrimaryCollectionContext> scheduler = PhaseWorkSchedulerFactory.ForPrimaryCollection(context))
				{
					using (MiniProfiler.Section("Scheduling"))
					{
						using (context.StatefulServices.Diagnostics.BeginCollectorStateDump(context, "AfterGenericsCollection"))
						{
							readOnlyGlobalPendingResults = new Unity.IL2CPP.AssemblyConversion.PrimaryCollection.Steps.Global.GenericsCollection(includeGenerics).Schedule(scheduler, assemblies);
						}
						new WarmNamingComponent().Schedule(scheduler, assemblies);
						using (context.StatefulServices.Diagnostics.BeginCollectorStateDump(context, "AfterSharedGenerics"))
						{
							readOnlyGlobalPendingResults2 = new GenericSharingAnalysis(includeGenerics).Schedule(scheduler, assemblies);
						}
						readOnlyPerAssemblyPendingResults = new AttributeSupportCollection().Schedule(scheduler, assemblies);
						readOnlyPerAssemblyPendingResults2 = new SequencePointCollection().Schedule(scheduler, assemblies);
						readOnlyPerAssemblyPendingResults3 = new CatchPointCollection().Schedule(scheduler, assemblies);
						readOnlyPerAssemblyPendingResults4 = new WindowsRuntimeDataCollection().Schedule(scheduler, assemblies);
						new CCWMarshalingFunctionCollection().Schedule(scheduler, assemblies);
						new AssemblyCollection().Schedule(scheduler, assemblies);
					}
				}
				using (MiniProfiler.Section("Build Results"))
				{
					context.Results.SetPrimaryCollectionResults(new AssemblyConversionResults.PrimaryCollectionPhase(new SequencePointProviderCollection(readOnlyPerAssemblyPendingResults2.Result), new CatchPointCollectorCollection(readOnlyPerAssemblyPendingResults3.Result), readOnlyGlobalPendingResults.Result, readOnlyPerAssemblyPendingResults.Result, context.Collectors.WindowsRuntimeTypeWithNames.Complete(), readOnlyPerAssemblyPendingResults4.Result, context.Collectors.CCWMarshallingFunctions.Complete(), readOnlyGlobalPendingResults2.Result));
				}
			}
		}
	}
}
