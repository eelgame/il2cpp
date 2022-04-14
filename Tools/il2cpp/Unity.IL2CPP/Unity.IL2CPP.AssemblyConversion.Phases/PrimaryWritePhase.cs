using System.Collections.ObjectModel;
using Mono.Cecil;
using Unity.IL2CPP.AssemblyConversion.PrimaryWrite.Steps.Global;
using Unity.IL2CPP.AssemblyConversion.PrimaryWrite.Steps.PerAssembly;
using Unity.IL2CPP.AssemblyConversion.Steps.Results;
using Unity.IL2CPP.Attributes;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Scheduling;
using Unity.IL2CPP.Tiny;
using Unity.MiniProfiling;

namespace Unity.IL2CPP.AssemblyConversion.Phases
{
	internal static class PrimaryWritePhase
	{
		public static void Run(AssemblyConversionContext context, ReadOnlyCollection<AssemblyDefinition> assemblies, AssemblyDefinition entryAssembly, bool includeGenerics = true)
		{
			using (MiniProfiler.Section("PrimaryWritePhase"))
			{
				ReadOnlyPerAssemblyPendingResults<ReadOnlyAttributeWriterOutput> readOnlyPerAssemblyPendingResults;
				ReadOnlyPerAssemblyPendingResults<TinyPrimaryWriteResult> readOnlyPerAssemblyPendingResults2;
				ReadOnlyGlobalPendingResults<TinyPrimaryWriteResult> readOnlyGlobalPendingResults;
				using (IPhaseWorkScheduler<GlobalWriteContext> scheduler = PhaseWorkSchedulerFactory.ForPrimaryWrite(context))
				{
					using (MiniProfiler.Section("Scheduling"))
					{
						using (context.StatefulServices.Diagnostics.BeginCollectorStateDump(context, "AfterWriteAssemblies"))
						{
							new WriteAssemblies().Schedule(scheduler, assemblies);
						}
						using (context.StatefulServices.Diagnostics.BeginCollectorStateDump(context, "AfterWriteAttributes"))
						{
							readOnlyPerAssemblyPendingResults = new WriteAttributes().Schedule(scheduler, assemblies);
						}
						using (context.StatefulServices.Diagnostics.BeginCollectorStateDump(context, "AfterWriteDriver"))
						{
							new WriteExecutableDriver(entryAssembly).Schedule(scheduler);
						}
						using (context.StatefulServices.Diagnostics.BeginCollectorStateDump(context, "AfterWriteGenerics"))
						{
							new WriteGenerics(includeGenerics).Schedule(scheduler);
						}
						using (context.StatefulServices.Diagnostics.BeginCollectorStateDump(context, "AfterWriteWindowsRuntimeFactories"))
						{
							new WriteWindowsRuntimeFactories().Schedule(scheduler);
						}
						using (context.StatefulServices.Diagnostics.BeginCollectorStateDump(context, "AfterWriteComCallableWrappers"))
						{
							new WriteComCallableWrappers().Schedule(scheduler);
						}
						using (context.StatefulServices.Diagnostics.BeginCollectorStateDump(context, "AfterWriteProjectedInterfacesByCCWs"))
						{
							new WriteProjectedInterfacesByComCallableWrappers().Schedule(scheduler);
						}
						using (context.StatefulServices.Diagnostics.BeginCollectorStateDump(context, "AfterWriteTinyPerAssemblyCode"))
						{
							readOnlyPerAssemblyPendingResults2 = new WriteTinyPerAssemblyCode().Schedule(scheduler, assemblies);
						}
						using (context.StatefulServices.Diagnostics.BeginCollectorStateDump(context, "AfterWriteTinyGenericsCode"))
						{
							readOnlyGlobalPendingResults = new WriteTinyGenericCode().Schedule(scheduler);
						}
					}
				}
				using (MiniProfiler.Section("Build Results"))
				{
					context.Results.SetPrimaryWritePhaseResults(new AssemblyConversionResults.PrimaryWritePhase(context.Collectors.SharedMethods.Complete(), context.Collectors.Methods.Complete(), readOnlyPerAssemblyPendingResults.Result, context.Collectors.ReversePInvokeWrappers.Complete(), context.Collectors.TypeMarshallingFunctions.Complete(), context.Collectors.WrappersForDelegateFromManagedToNative.Complete(), context.Collectors.InteropGuids.Complete(), context.Collectors.MetadataUsage.Complete(), readOnlyPerAssemblyPendingResults2.Result, readOnlyGlobalPendingResults.Result, context.Collectors.TinyTypeCollector.Complete(), context.Collectors.TinyStringCollector.Complete(), context.Collectors.GenericMethodCollector.Complete()));
				}
			}
		}
	}
}
