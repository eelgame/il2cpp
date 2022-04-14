using System.Collections.ObjectModel;
using Mono.Cecil;
using Unity.IL2CPP.Contexts.Scheduling;
using Unity.MiniProfiling;

namespace Unity.IL2CPP.AssemblyConversion.Steps.Base
{
	public abstract class GlobalScheduledStepAction<TWorkerContext, TGlobalState> : BaseScheduledItemsStep<TWorkerContext, AssemblyDefinition>
	{
		protected abstract void ProcessItem(TWorkerContext context, AssemblyDefinition item, TGlobalState globalState);

		protected abstract TGlobalState CreateGlobalState(TWorkerContext context);

		public void Schedule(IPhaseWorkScheduler<TWorkerContext> scheduler, ReadOnlyCollection<AssemblyDefinition> items)
		{
			TWorkerContext contextForMainThread = scheduler.ContextForMainThread;
			if (!Skip(contextForMainThread))
			{
				scheduler.Enqueue<TWorkerContext, ReadOnlyCollection<AssemblyDefinition>, object>(contextForMainThread, items, WorkerWrapper, null);
			}
		}

		private void WorkerWrapper(WorkItemData<TWorkerContext, ReadOnlyCollection<AssemblyDefinition>, object> workerData)
		{
			using (MiniProfiler.Section(StepUtilities.FormatAllStepName(Name)))
			{
				ProcessAllItems(workerData.Context, workerData.Item);
			}
		}

		protected virtual void ProcessAllItems(TWorkerContext context, ReadOnlyCollection<AssemblyDefinition> items)
		{
			TGlobalState globalState = CreateGlobalState(context);
			foreach (AssemblyDefinition item in items)
			{
				using (CreateProfilerSectionForProcessItem(item))
				{
					ProcessItem(context, item, globalState);
				}
			}
		}

		protected override string ProfilerDetailsForItem(AssemblyDefinition workerItem)
		{
			return workerItem.Name.Name;
		}
	}
}
