using System.Collections.ObjectModel;
using Unity.IL2CPP.AssemblyConversion.Steps.Results;
using Unity.IL2CPP.Contexts.Scheduling;
using Unity.IL2CPP.Contexts.Services;
using Unity.MiniProfiling;

namespace Unity.IL2CPP.AssemblyConversion.Steps.Base
{
	public abstract class ScheduledItemsStepFuncWithContinueFunc<TWorkerContext, TWorkerItem, TWorkerResult, TContinueResult> : BaseScheduledItemsStep<TWorkerContext, TWorkerItem>
	{
		protected abstract string PostProcessingSectionName { get; }

		protected abstract TWorkerResult ProcessItem(TWorkerContext context, TWorkerItem item);

		public ReadOnlyGlobalPendingResults<TContinueResult> Schedule(IPhaseWorkScheduler<TWorkerContext> scheduler, ReadOnlyCollection<TWorkerItem> items)
		{
			using (CreateProfilerSectionAroundScheduling(scheduler.WorkIsDoneOnDifferentThread))
			{
				TWorkerContext contextForMainThread = scheduler.ContextForMainThread;
				GlobalPendingResults<TContinueResult> globalPendingResults = new GlobalPendingResults<TContinueResult>();
				if (Skip(contextForMainThread))
				{
					globalPendingResults.SetResults(CreateEmptyResult());
					return new ReadOnlyGlobalPendingResults<TContinueResult>(globalPendingResults);
				}
				scheduler.EnqueueItemsAndContinueWithResults(contextForMainThread, items, WorkerWrapper, PostProcessWrapper, globalPendingResults);
				return new ReadOnlyGlobalPendingResults<TContinueResult>(globalPendingResults);
			}
		}

		protected abstract TContinueResult CreateEmptyResult();

		private void PostProcessWrapper(WorkItemData<TWorkerContext, ReadOnlyCollection<ResultData<TWorkerItem, TWorkerResult>>, GlobalPendingResults<TContinueResult>> workerData)
		{
			using (MiniProfiler.Section(PostProcessingSectionName))
			{
				TContinueResult results = PostProcess(workerData.Context, workerData.Item);
				workerData.Tag.SetResults(results);
			}
		}

		protected abstract TContinueResult PostProcess(TWorkerContext context, ReadOnlyCollection<ResultData<TWorkerItem, TWorkerResult>> data);

		private TWorkerResult WorkerWrapper(WorkItemData<TWorkerContext, TWorkerItem, GlobalPendingResults<TContinueResult>> workerData)
		{
			using (CreateProfilerSectionForProcessItem(workerData.Item))
			{
				return ProcessItem(workerData.Context, workerData.Item);
			}
		}
	}
}
