using System.Collections.ObjectModel;
using Unity.IL2CPP.Contexts.Scheduling;
using Unity.IL2CPP.Contexts.Services;
using Unity.MiniProfiling;

namespace Unity.IL2CPP.AssemblyConversion.Steps.Base
{
	public abstract class ScheduledItemsStepFuncWithContinueAction<TWorkerContext, TWorkerItem, TWorkerResult> : BaseScheduledItemsStep<TWorkerContext, TWorkerItem>
	{
		protected abstract string PostProcessingSectionName { get; }

		protected abstract TWorkerResult ProcessItem(TWorkerContext context, TWorkerItem item);

		public void Schedule(IPhaseWorkScheduler<TWorkerContext> scheduler, ReadOnlyCollection<TWorkerItem> items)
		{
			using (CreateProfilerSectionAroundScheduling(scheduler.WorkIsDoneOnDifferentThread))
			{
				TWorkerContext contextForMainThread = scheduler.ContextForMainThread;
				if (!Skip(contextForMainThread))
				{
					scheduler.EnqueueItemsAndContinueWithResults<TWorkerContext, TWorkerItem, TWorkerResult, object>(contextForMainThread, items, WorkerWrapper, PostProcessWrapper, null);
				}
			}
		}

		private void PostProcessWrapper(WorkItemData<TWorkerContext, ReadOnlyCollection<ResultData<TWorkerItem, TWorkerResult>>, object> workerData)
		{
			using (MiniProfiler.Section(PostProcessingSectionName))
			{
				PostProcess(workerData.Context, workerData.Item);
			}
		}

		protected abstract void PostProcess(TWorkerContext context, ReadOnlyCollection<ResultData<TWorkerItem, TWorkerResult>> data);

		private TWorkerResult WorkerWrapper(WorkItemData<TWorkerContext, TWorkerItem, object> workerData)
		{
			using (CreateProfilerSectionForProcessItem(workerData.Item))
			{
				return ProcessItem(workerData.Context, workerData.Item);
			}
		}
	}
}
