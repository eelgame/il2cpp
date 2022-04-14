using System;
using System.Collections.Generic;
using Unity.IL2CPP.Contexts.Forking.Steps;
using Unity.IL2CPP.Contexts.Scheduling;
using Unity.IL2CPP.Contexts.Services;

namespace Unity.IL2CPP.Contexts.Components
{
	public class RealSchedulerComponent : ImmediateSchedulerComponent, IForkableComponent<IWorkScheduler, object, ImmediateSchedulerComponent>
	{
		private IWorkScheduler _scheduler;

		public void Initialize(IWorkScheduler scheduler)
		{
			_scheduler = scheduler;
		}

		public override void Enqueue<TWorkerContext>(TWorkerContext context, Action<WorkItemData<TWorkerContext>> action)
		{
			_scheduler.Enqueue(context, action);
		}

		public override void Enqueue<TWorkerContext, TItem, TTag>(TWorkerContext context, TItem item, WorkerAction<TWorkerContext, TItem, TTag> action, TTag tag)
		{
			_scheduler.Enqueue(context, item, action, tag);
		}

		public override void Enqueue<TWorkerContext, TTag>(TWorkerContext context, Action<WorkItemData<TWorkerContext, TTag>> action, TTag tag)
		{
			_scheduler.Enqueue(context, action, tag);
		}

		public override void Enqueue<TWorkerContext>(TWorkerContext context, Action<TWorkerContext> action)
		{
			_scheduler.Enqueue(context, action);
		}

		public override void EnqueueItems<TWorkerContext, TItem, TTag>(TWorkerContext context, ICollection<TItem> items, WorkerAction<TWorkerContext, TItem, TTag> action, TTag tag)
		{
			_scheduler.EnqueueItems(context, items, action, tag);
		}

		public override void EnqueueItemsAndContinueWith<TWorkerContext, TItem, TTag>(TWorkerContext context, ICollection<TItem> items, WorkerAction<TWorkerContext, TItem, TTag> action, ContinueAction<TWorkerContext, TTag> continueAction, TTag tag)
		{
			_scheduler.EnqueueItemsAndContinueWith(context, items, action, continueAction, tag);
		}

		public override void EnqueueItemsAndContinueWithResults<TWorkerContext, TItem, TResult, TTag>(TWorkerContext context, ICollection<TItem> items, WorkerFunc<TWorkerContext, TItem, TResult, TTag> func, ContinueWithCollectionResults<TWorkerContext, TItem, TResult, TTag> continueWithResults, TTag tag)
		{
			_scheduler.EnqueueItemsAndContinueWithResults(context, items, func, continueWithResults, tag);
		}

		void IForkableComponent<IWorkScheduler, object, ImmediateSchedulerComponent>.ForkForPrimaryWrite(PrimaryWriteAssembliesLateAccessForkingContainer lateAccess, out IWorkScheduler writer, out object reader, out ImmediateSchedulerComponent full)
		{
			writer = (full = this);
			reader = null;
		}

		void IForkableComponent<IWorkScheduler, object, ImmediateSchedulerComponent>.MergeForPrimaryWrite(ImmediateSchedulerComponent forked)
		{
		}
	}
}
