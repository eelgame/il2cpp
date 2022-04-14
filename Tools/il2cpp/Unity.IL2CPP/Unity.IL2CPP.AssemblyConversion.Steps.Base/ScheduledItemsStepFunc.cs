using System.Collections.ObjectModel;
using Unity.IL2CPP.AssemblyConversion.Steps.Results;
using Unity.IL2CPP.Contexts.Scheduling;
using Unity.IL2CPP.Contexts.Services;

namespace Unity.IL2CPP.AssemblyConversion.Steps.Base
{
	public abstract class ScheduledItemsStepFunc<TWorkerContext, TWorkerItem, TWorkerResult, TPendingResult, TReadOnlyPendingResults> : BaseScheduledItemsStep<TWorkerContext, TWorkerItem> where TPendingResult : PendingResults<TWorkerItem, TWorkerResult> where TReadOnlyPendingResults : ReadOnlyPendingResults<TWorkerItem, TWorkerResult>
	{
		protected abstract TWorkerResult ProcessItem(TWorkerContext context, TWorkerItem item);

		public TReadOnlyPendingResults Schedule(IPhaseWorkScheduler<TWorkerContext> scheduler, ReadOnlyCollection<TWorkerItem> items)
		{
			using (CreateProfilerSectionAroundScheduling(scheduler.WorkIsDoneOnDifferentThread))
			{
				TWorkerContext contextForMainThread = scheduler.ContextForMainThread;
				TPendingResult val = CreateScheduleResult();
				if (Skip(contextForMainThread))
				{
					val.SetResultsAsEmpty();
					return CreateReadOnlyPendingResults(val);
				}
				scheduler.EnqueueItemsAndContinueWithResults(contextForMainThread, items, WorkerWrapper, delegate(WorkItemData<TWorkerContext, ReadOnlyCollection<ResultData<TWorkerItem, TWorkerResult>>, TPendingResult> resultWorkerData)
				{
					resultWorkerData.Tag.SetResults(resultWorkerData.Item);
				}, val);
				return CreateReadOnlyPendingResults(val);
			}
		}

		protected abstract TPendingResult CreateScheduleResult();

		protected abstract TReadOnlyPendingResults CreateReadOnlyPendingResults(TPendingResult pendingResults);

		private TWorkerResult WorkerWrapper(WorkItemData<TWorkerContext, TWorkerItem, TPendingResult> workerData)
		{
			using (CreateProfilerSectionForProcessItem(workerData.Item))
			{
				return ProcessItem(workerData.Context, workerData.Item);
			}
		}
	}
}
