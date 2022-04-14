using System.Collections.ObjectModel;
using Unity.IL2CPP.Contexts.Scheduling;

namespace Unity.IL2CPP.AssemblyConversion.Steps.Base
{
	public abstract class ScheduledItemsStepAction<TWorkerContext, TWorkerItem> : BaseScheduledItemsStep<TWorkerContext, TWorkerItem>
	{
		protected abstract void ProcessItem(TWorkerContext context, TWorkerItem item);

		public void Schedule(IPhaseWorkScheduler<TWorkerContext> scheduler, ReadOnlyCollection<TWorkerItem> items)
		{
			using (CreateProfilerSectionAroundScheduling(scheduler.WorkIsDoneOnDifferentThread))
			{
				TWorkerContext contextForMainThread = scheduler.ContextForMainThread;
				if (!Skip(contextForMainThread))
				{
					scheduler.EnqueueItems<TWorkerContext, TWorkerItem, object>(contextForMainThread, items, WorkerWrapper, null);
				}
			}
		}

		private void WorkerWrapper(WorkItemData<TWorkerContext, TWorkerItem, object> workerData)
		{
			using (CreateProfilerSectionForProcessItem(workerData.Item))
			{
				ProcessItem(workerData.Context, workerData.Item);
			}
		}
	}
}
