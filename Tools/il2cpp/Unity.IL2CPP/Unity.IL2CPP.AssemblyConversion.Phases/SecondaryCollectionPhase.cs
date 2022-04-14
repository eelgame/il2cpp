using System.Collections.ObjectModel;
using Mono.Cecil;
using Unity.IL2CPP.AssemblyConversion.SecondaryCollection.Steps.Generics;
using Unity.IL2CPP.AssemblyConversion.SecondaryCollection.Steps.Global;
using Unity.IL2CPP.AssemblyConversion.SecondaryCollection.Steps.PerAssembly;
using Unity.IL2CPP.AssemblyConversion.Steps.Results;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Results;
using Unity.IL2CPP.Contexts.Scheduling;
using Unity.IL2CPP.Metadata;
using Unity.IL2CPP.Tiny;
using Unity.MiniProfiling;

namespace Unity.IL2CPP.AssemblyConversion.Phases
{
	internal static class SecondaryCollectionPhase
	{
		public static void Run(AssemblyConversionContext context, ReadOnlyCollection<AssemblyDefinition> assemblies)
		{
			ReadOnlyGlobalPendingResults<ReadOnlyInvokerCollection> readOnlyGlobalPendingResults = null;
			ReadOnlyGlobalPendingResults<ReadOnlyMethodTables> readOnlyGlobalPendingResults2 = null;
			ReadOnlyGlobalPendingResults<IMetadataCollectionResults> readOnlyGlobalPendingResults3 = null;
			ITinyTypeMetadataResults tinyTypeMetadata = null;
			ITinyStringMetadataResults tinyStringMetadata = null;
			ReadOnlyPerAssemblyPendingResults<GenericContextCollection> readOnlyPerAssemblyPendingResults = null;
			using (MiniProfiler.Section("SecondaryCollectionPhase"))
			{
				using (MiniProfiler.Section("Scheduling"))
				{
					if (!context.Parameters.UsingTinyBackend)
					{
						using (IPhaseWorkScheduler<GlobalSecondaryCollectionContext> phaseWorkScheduler = PhaseWorkSchedulerFactory.ForSecondaryCollection(context))
						{
							using (context.StatefulServices.Diagnostics.BeginCollectorStateDump(context, "AfterCollectGenericContextMetadata"))
							{
								readOnlyPerAssemblyPendingResults = new CollectGenericContextMetadata().Schedule(phaseWorkScheduler, assemblies);
							}
							using (context.StatefulServices.Diagnostics.BeginCollectorStateDump(context, "AfterCollectMetadata"))
							{
								readOnlyGlobalPendingResults3 = new CollectMetadata().Schedule(phaseWorkScheduler, assemblies);
							}
							readOnlyGlobalPendingResults = new CollectInvokers().Schedule(phaseWorkScheduler, assemblies, phaseWorkScheduler.ContextForMainThread.Results.PrimaryWrite.GenericMethods.UnsortedKeys);
							readOnlyGlobalPendingResults2 = new CollectMethodTables().Schedule(phaseWorkScheduler);
						}
					}
					else
					{
						tinyTypeMetadata = TinyTypeMetadataCollector.Collect(context.CreateReadOnlyContext(), context.Results.PrimaryWrite.TinyTypes);
						tinyStringMetadata = TinyStringMetadataCollector.Collect(context.CreateReadOnlyContext(), context.Results.PrimaryWrite.TinyStrings);
					}
				}
				using (MiniProfiler.Section("Build Results"))
				{
					context.Results.SetSecondaryCollectionPhaseResults(new AssemblyConversionResults.SecondaryCollectionPhase(readOnlyGlobalPendingResults?.Result, readOnlyGlobalPendingResults2?.Result, readOnlyPerAssemblyPendingResults?.Result, readOnlyGlobalPendingResults3?.Result, context.Collectors.TypeCollector.Complete(), tinyTypeMetadata, tinyStringMetadata));
				}
			}
		}
	}
}
