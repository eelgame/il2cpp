using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Unity.IL2CPP.Contexts.Components.Base;
using Unity.IL2CPP.Contexts.Forking.Steps;
using Unity.IL2CPP.Contexts.Scheduling;
using Unity.IL2CPP.Contexts.Services;

namespace Unity.IL2CPP.Contexts.Components
{
	public class ImmediateSchedulerComponent : StatelessComponentBase<IWorkScheduler, object, ImmediateSchedulerComponent>, IWorkScheduler
	{
		public virtual void Enqueue<TWorkerContext>(TWorkerContext context, Action<WorkItemData<TWorkerContext>> action)
		{
			action(new WorkItemData<TWorkerContext>(context, 0));
		}

		public virtual void Enqueue<TWorkerContext, TTag>(TWorkerContext context, Action<WorkItemData<TWorkerContext, TTag>> action, TTag tag)
		{
			action(new WorkItemData<TWorkerContext, TTag>(context, 0, tag));
		}

		public virtual void Enqueue<TWorkerContext>(TWorkerContext context, Action<TWorkerContext> action)
		{
			action(context);
		}

		public virtual void Enqueue<TWorkerContext, TItem, TTag>(TWorkerContext context, TItem item, WorkerAction<TWorkerContext, TItem, TTag> action, TTag tag)
		{
			action(new WorkItemData<TWorkerContext, TItem, TTag>(context, item, 0, tag));
		}

		public virtual void EnqueueItems<TWorkerContext, TItem, TTag>(TWorkerContext context, ICollection<TItem> items, WorkerAction<TWorkerContext, TItem, TTag> action, TTag tag)
		{
			foreach (TItem item in items)
			{
				action(new WorkItemData<TWorkerContext, TItem, TTag>(context, item, 0, tag));
			}
		}

		public virtual void EnqueueItemsAndContinueWithResults<TWorkerContext, TItem, TResult, TTag>(TWorkerContext context, ICollection<TItem> items, WorkerFunc<TWorkerContext, TItem, TResult, TTag> func, ContinueWithCollectionResults<TWorkerContext, TItem, TResult, TTag> continueWithResults, TTag tag)
		{
			List<ResultData<TItem, TResult>> list = new List<ResultData<TItem, TResult>>();
			foreach (TItem item in items)
			{
				TResult result = func(new WorkItemData<TWorkerContext, TItem, TTag>(context, item, 0, tag));
				list.Add(new ResultData<TItem, TResult>(item, result));
			}
			continueWithResults(new WorkItemData<TWorkerContext, ReadOnlyCollection<ResultData<TItem, TResult>>, TTag>(context, list.AsReadOnly(), 0, tag));
		}

		public virtual void EnqueueItemsAndContinueWith<TWorkerContext, TItem, TTag>(TWorkerContext context, ICollection<TItem> items, WorkerAction<TWorkerContext, TItem, TTag> action, ContinueAction<TWorkerContext, TTag> continueAction, TTag tag)
		{
			foreach (TItem item in items)
			{
				action(new WorkItemData<TWorkerContext, TItem, TTag>(context, item, 0, tag));
			}
			continueAction(new WorkItemData<TWorkerContext, TTag>(context, 0, tag));
		}

		protected override ImmediateSchedulerComponent ThisAsFull()
		{
			return this;
		}

		protected override object ThisAsRead()
		{
			throw new NotSupportedException();
		}

		protected override IWorkScheduler GetNotAvailableWrite()
		{
			throw new NotSupportedException();
		}

		protected override object GetNotAvailableRead()
		{
			throw new NotSupportedException();
		}

		protected override void ForkForSecondaryCollection(SecondaryCollectionLateAccessForkingContainer lateAccess, out IWorkScheduler writer, out object reader, out ImmediateSchedulerComponent full)
		{
			WriteOnlyFork(out writer, out reader, out full);
		}

		protected override void ForkForPrimaryWrite(PrimaryWriteAssembliesLateAccessForkingContainer lateAccess, out IWorkScheduler writer, out object reader, out ImmediateSchedulerComponent full)
		{
			WriteOnlyFork(out writer, out reader, out full);
		}

		protected override void ForkForPrimaryCollection(PrimaryCollectionLateAccessForkingContainer lateAccess, out IWorkScheduler writer, out object reader, out ImmediateSchedulerComponent full)
		{
			WriteOnlyFork(out writer, out reader, out full);
		}

		protected override void ForkForSecondaryWrite(SecondaryWriteLateAccessForkingContainer lateAccess, out IWorkScheduler writer, out object reader, out ImmediateSchedulerComponent full)
		{
			WriteOnlyFork(out writer, out reader, out full);
		}
	}
}
