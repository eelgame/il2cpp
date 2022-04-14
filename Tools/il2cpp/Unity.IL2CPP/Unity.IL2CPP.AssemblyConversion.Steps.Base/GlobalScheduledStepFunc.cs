using System.Collections.ObjectModel;
using Mono.Cecil;
using Unity.IL2CPP.AssemblyConversion.Steps.Results;
using Unity.IL2CPP.Contexts.Scheduling;
using Unity.MiniProfiling;

namespace Unity.IL2CPP.AssemblyConversion.Steps.Base
{
	public abstract class GlobalScheduledStepFunc<TWorkerContext, TGlobalState, TResult> : BaseScheduledItemsStep<TWorkerContext, AssemblyDefinition>
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
			scheduler.Enqueue(contextForMainThread, items, WorkerWrapper, globalPendingResults);
			return new ReadOnlyGlobalPendingResults<TResult>(globalPendingResults);
		}

		protected abstract void ProcessItem(TWorkerContext context, AssemblyDefinition item, TGlobalState globalState);

		protected abstract TResult CreateEmptyResult();

		protected abstract TGlobalState CreateGlobalState(TWorkerContext context);

		protected abstract TResult GetResults(TWorkerContext context, TGlobalState globalState);

		private void WorkerWrapper(WorkItemData<TWorkerContext, ReadOnlyCollection<AssemblyDefinition>, GlobalPendingResults<TResult>> workerData)
		{
			using (MiniProfiler.Section(StepUtilities.FormatAllStepName(Name)))
			{
				TResult results = ProcessAllItems(workerData.Context, workerData.Item);
				workerData.Tag.SetResults(results);
			}
		}

		protected virtual TResult ProcessAllItems(TWorkerContext context, ReadOnlyCollection<AssemblyDefinition> items)
		{
			TGlobalState globalState = CreateGlobalState(context);
			foreach (AssemblyDefinition item in items)
			{
				using (CreateProfilerSectionForProcessItem(item))
				{
					ProcessItem(context, item, globalState);
				}
			}
			return GetResults(context, globalState);
		}

		protected override string ProfilerDetailsForItem(AssemblyDefinition workerItem)
		{
			return workerItem.Name.Name;
		}
	}
}
