using Unity.IL2CPP.Contexts.Scheduling;
using Unity.MiniProfiling;

namespace Unity.IL2CPP.AssemblyConversion.Steps.Base
{
	public abstract class SimpleScheduledStep<TWorkerContext> : ScheduledStep<TWorkerContext>
	{
		public void Schedule(IPhaseWorkScheduler<TWorkerContext> scheduler)
		{
			TWorkerContext contextForMainThread = scheduler.ContextForMainThread;
			if (!Skip(contextForMainThread))
			{
				scheduler.Enqueue<TWorkerContext, object>(contextForMainThread, WorkerWrapper, null);
			}
		}

		private void WorkerWrapper(WorkItemData<TWorkerContext, object> workerData)
		{
			using (MiniProfiler.Section(Name))
			{
				Worker(workerData.Context);
			}
		}

		protected abstract void Worker(TWorkerContext context);
	}
}
