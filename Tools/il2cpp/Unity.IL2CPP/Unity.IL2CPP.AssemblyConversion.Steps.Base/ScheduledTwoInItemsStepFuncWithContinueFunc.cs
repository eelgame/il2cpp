using System;
using System.Collections.ObjectModel;
using System.Linq;
using Unity.IL2CPP.AssemblyConversion.Steps.Results;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts.Scheduling;
using Unity.IL2CPP.Contexts.Services;
using Unity.MiniProfiling;

namespace Unity.IL2CPP.AssemblyConversion.Steps.Base
{
	public abstract class ScheduledTwoInItemsStepFuncWithContinueFunc<TWorkerContext, TWorkerItem, TWorkerItem2, TWorkerResult, TContinueResult> : BaseScheduledItemsStep<TWorkerContext, TWorkerItem>
	{
		protected abstract string PostProcessingSectionName { get; }

		protected abstract TWorkerResult ProcessItem(TWorkerContext context, TWorkerItem item);

		protected abstract TWorkerResult ProcessItem(TWorkerContext context, TWorkerItem2 item);

		protected abstract string ProfilerDetailsForItem2(TWorkerItem2 workerItem);

		protected IDisposable CreateProfilerSectionForProcessItem2(TWorkerItem2 item)
		{
			return MiniProfiler.Section(Name, ProfilerDetailsForItem2(item));
		}

		public ReadOnlyGlobalPendingResults<TContinueResult> Schedule(IPhaseWorkScheduler<TWorkerContext> scheduler, ReadOnlyCollection<TWorkerItem> items, TWorkerItem2 item2)
		{
			return Schedule(scheduler, items, new TWorkerItem2[1] { item2 }.AsReadOnly());
		}

		public ReadOnlyGlobalPendingResults<TContinueResult> Schedule(IPhaseWorkScheduler<TWorkerContext> scheduler, ReadOnlyCollection<TWorkerItem> items, ReadOnlyCollection<TWorkerItem2> items2)
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
				scheduler.EnqueueItemsAndContinueWithResults(contextForMainThread, OrderItemsForScheduling(items, items2), WorkerWrapper, PostProcessWrapper, globalPendingResults);
				return new ReadOnlyGlobalPendingResults<TContinueResult>(globalPendingResults);
			}
		}

		protected abstract TContinueResult CreateEmptyResult();

		protected abstract ReadOnlyCollection<object> OrderItemsForScheduling(ReadOnlyCollection<TWorkerItem> items, ReadOnlyCollection<TWorkerItem2> items2);

		private void PostProcessWrapper(WorkItemData<TWorkerContext, ReadOnlyCollection<ResultData<object, TWorkerResult>>, GlobalPendingResults<TContinueResult>> workerData)
		{
			using (MiniProfiler.Section(PostProcessingSectionName))
			{
				TContinueResult results = PostProcess(workerData.Context, workerData.Item.Select((ResultData<object, TWorkerResult> r) => r.Result).ToList().AsReadOnly());
				workerData.Tag.SetResults(results);
			}
		}

		protected abstract TContinueResult PostProcess(TWorkerContext context, ReadOnlyCollection<TWorkerResult> data);

		private TWorkerResult WorkerWrapper(WorkItemData<TWorkerContext, object, GlobalPendingResults<TContinueResult>> workerData)
		{
			if (workerData.Item is TWorkerItem item)
			{
				using (CreateProfilerSectionForProcessItem(item))
				{
					return ProcessItem(workerData.Context, item);
				}
			}
			TWorkerItem2 item2 = (TWorkerItem2)workerData.Item;
			using (CreateProfilerSectionForProcessItem2(item2))
			{
				return ProcessItem(workerData.Context, item2);
			}
		}
	}
}
