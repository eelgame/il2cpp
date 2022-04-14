using Unity.IL2CPP.Contexts.Scheduling;

namespace Unity.IL2CPP.AssemblyConversion.Steps.Base
{
	public abstract class CustomScheduledStep<TWorkerContext> : ScheduledStep<TWorkerContext>
	{
		public void Schedule(IPhaseWorkScheduler<TWorkerContext> scheduler)
		{
			using (CreateProfilerSectionAroundScheduling(scheduler.WorkIsDoneOnDifferentThread))
			{
				TWorkerContext contextForMainThread = scheduler.ContextForMainThread;
				if (!Skip(contextForMainThread))
				{
					DoScheduling(contextForMainThread);
				}
			}
		}

		protected abstract void DoScheduling(TWorkerContext context);
	}
}
