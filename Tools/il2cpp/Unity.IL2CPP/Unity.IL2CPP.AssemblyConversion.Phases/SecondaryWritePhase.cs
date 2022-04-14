using System.Collections.ObjectModel;
using Mono.Cecil;
using Unity.IL2CPP.AssemblyConversion.SecondaryWrite.Steps.Global;
using Unity.IL2CPP.AssemblyConversion.SecondaryWrite.Steps.PerAssembly;
using Unity.IL2CPP.AssemblyConversion.Steps;
using Unity.IL2CPP.AssemblyConversion.Steps.Results;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Scheduling;
using Unity.IL2CPP.StringLiterals;
using Unity.MiniProfiling;

namespace Unity.IL2CPP.AssemblyConversion.Phases
{
	internal static class SecondaryWritePhase
	{
		public static void Run(AssemblyConversionContext context, ReadOnlyCollection<AssemblyDefinition> assemblies, bool includeDebuggerTables = true, bool includeMetadata = true)
		{
			using (MiniProfiler.Section("SecondaryWritePhase"))
			{
				Part1(context, assemblies, includeDebuggerTables);
				Part3(context);
				Part4(context, assemblies, includeMetadata);
			}
		}

		private static void Part1(AssemblyConversionContext context, ReadOnlyCollection<AssemblyDefinition> assemblies, bool includeDebuggerTables = true)
		{
			using (MiniProfiler.Section("Part1"))
			{
				using (MiniProfiler.Section("Scheduling"))
				{
					if (includeDebuggerTables)
					{
						using (context.StatefulServices.Diagnostics.BeginCollectorStateDump(context, "AfterWriteDebuggerTables"))
						{
							SecondaryWriteSteps.WriteDebuggerTables(context.GlobalWriteContext, assemblies);
						}
					}
				}
				using (MiniProfiler.Section("Build Results"))
				{
					context.Results.SetSecondaryWritePhasePart1Results(new AssemblyConversionResults.SecondaryWritePhasePart1(context.Collectors.VirtualCalls.Complete()));
				}
			}
		}

		private static void Part3(AssemblyConversionContext context)
		{
			using (MiniProfiler.Section("Part3"))
			{
				UnresolvedVirtualsTablesInfo virtualCallTables;
				using (MiniProfiler.Section("Scheduling"))
				{
					using (context.StatefulServices.Diagnostics.BeginCollectorStateDump(context, "AfterWriteUnresolvedStubs"))
					{
						SecondaryWriteSteps.WriteUnresolvedVirtualCalls(context.GlobalWriteContext, out virtualCallTables);
					}
				}
				using (MiniProfiler.Section("Build Results"))
				{
					context.Results.SetSecondaryWritePhasePart3Results(new AssemblyConversionResults.SecondaryWritePhasePart3(virtualCallTables));
				}
			}
		}

		private static void Part4(AssemblyConversionContext context, ReadOnlyCollection<AssemblyDefinition> assemblies, bool includeMetadata = true)
		{
			using (MiniProfiler.Section("Part4"))
			{
				ReadOnlyGlobalPendingResults<(IStringLiteralCollection, IFieldReferenceCollection)> readOnlyGlobalPendingResults = null;
				using (MiniProfiler.Section("Scheduling"))
				{
					if (includeMetadata)
					{
						using (IPhaseWorkScheduler<GlobalWriteContext> scheduler = PhaseWorkSchedulerFactory.ForSecondaryWrite(context))
						{
							using (context.StatefulServices.Diagnostics.BeginCollectorStateDump(context, "AfterWriteMetadata"))
							{
								readOnlyGlobalPendingResults = new WriteGlobalMetadata().Schedule(scheduler, assemblies);
							}
							using (context.StatefulServices.Diagnostics.BeginCollectorStateDump(context, "AfterWriteCodeMetadata"))
							{
								new WritePerAssemblyCodeMetadata().Schedule(scheduler, assemblies);
							}
						}
					}
				}
				using (MiniProfiler.Section("Build Results"))
				{
					context.Results.SetSecondaryWritePhaseResults(new AssemblyConversionResults.SecondaryWritePhase(readOnlyGlobalPendingResults.Result.Item1, readOnlyGlobalPendingResults.Result.Item2, context.Collectors.Symbols.Complete()));
				}
			}
		}
	}
}
