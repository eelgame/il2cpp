using System.Collections.ObjectModel;
using Mono.Cecil;
using Unity.IL2CPP.AssemblyConversion.Steps.Results;
using Unity.IL2CPP.Contexts.Scheduling;
using Unity.MiniProfiling;

namespace Unity.IL2CPP.AssemblyConversion.Steps.Base
{
	public abstract class GlobalSimpleScheduledStepFunc<TWorkerContext, TResult> : ScheduledStep<TWorkerContext>
	{
		public ReadOnlyGlobalPendingResults<TResult> Schedule(IPhaseWorkScheduler<TWorkerContext> scheduler, ReadOnlyCollection<AssemblyDefinition> items)
		{
			TWorkerContext contextForMainThread = scheduler.ContextForMainThread;
			GlobalPendingResults<TResult> globalPendingResults = new GlobalPendingResults<TResult>();
			if (Skip(contextForMainThread))
			{
				globalPendingResults.SetResults(CreateEmptyResult());
				return new ReadOnlyGlobalPendingResults<TResult>(globalPendingResults);
			}
			scheduler.Enqueue(contextForMainThread, WorkerWrapper, (globalPendingResults, items));
			return new ReadOnlyGlobalPendingResults<TResult>(globalPendingResults);
		}

		protected abstract TResult CreateEmptyResult();

		private void WorkerWrapper(WorkItemData<TWorkerContext, (GlobalPendingResults<TResult>, ReadOnlyCollection<AssemblyDefinition>)> workerData)
		{
			using (MiniProfiler.Section(Name))
			{
				TResult results = Worker(workerData.Context, workerData.Tag.Item2);
				workerData.Tag.Item1.SetResults(results);
			}
		}

		protected abstract TResult Worker(TWorkerContext context, ReadOnlyCollection<AssemblyDefinition> assemblies);
	}
}
