using Unity.IL2CPP.AssemblyConversion.Steps.Results;
using Unity.IL2CPP.Contexts.Scheduling;
using Unity.MiniProfiling;

namespace Unity.IL2CPP.AssemblyConversion.Steps.Base
{
	public abstract class SimpleScheduledStepFunc<TWorkerContext, TResult> : ScheduledStep<TWorkerContext>
	{
		public ReadOnlyGlobalPendingResults<TResult> Schedule(IPhaseWorkScheduler<TWorkerContext> scheduler)
		{
			TWorkerContext contextForMainThread = scheduler.ContextForMainThread;
			GlobalPendingResults<TResult> globalPendingResults = new GlobalPendingResults<TResult>();
			if (Skip(contextForMainThread))
			{
				globalPendingResults.SetResults(CreateEmptyResult());
				return new ReadOnlyGlobalPendingResults<TResult>(globalPendingResults);
			}
			scheduler.Enqueue(contextForMainThread, WorkerWrapper, globalPendingResults);
			return new ReadOnlyGlobalPendingResults<TResult>(globalPendingResults);
		}

		protected abstract TResult CreateEmptyResult();

		private void WorkerWrapper(WorkItemData<TWorkerContext, GlobalPendingResults<TResult>> workerData)
		{
			using (MiniProfiler.Section(Name))
			{
				TResult results = Worker(workerData.Context);
				workerData.Tag.SetResults(results);
			}
		}

		protected abstract TResult Worker(TWorkerContext context);
	}
}
