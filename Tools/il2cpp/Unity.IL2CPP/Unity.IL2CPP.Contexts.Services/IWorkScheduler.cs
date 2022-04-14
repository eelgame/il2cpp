using System;
using System.Collections.Generic;
using Unity.IL2CPP.Contexts.Scheduling;

namespace Unity.IL2CPP.Contexts.Services
{
	public interface IWorkScheduler
	{
		void Enqueue<TWorkerContext>(TWorkerContext context, Action<TWorkerContext> action);

		void Enqueue<TWorkerContext>(TWorkerContext context, Action<WorkItemData<TWorkerContext>> action);

		void Enqueue<TWorkerContext, TTag>(TWorkerContext context, Action<WorkItemData<TWorkerContext, TTag>> action, TTag tag);

		void Enqueue<TWorkerContext, TItem, TTag>(TWorkerContext context, TItem item, WorkerAction<TWorkerContext, TItem, TTag> action, TTag tag);

		void EnqueueItems<TWorkerContext, TItem, TTag>(TWorkerContext context, ICollection<TItem> items, WorkerAction<TWorkerContext, TItem, TTag> action, TTag tag);

		void EnqueueItemsAndContinueWithResults<TWorkerContext, TItem, TResult, TTag>(TWorkerContext context, ICollection<TItem> items, WorkerFunc<TWorkerContext, TItem, TResult, TTag> func, ContinueWithCollectionResults<TWorkerContext, TItem, TResult, TTag> continueWithResults, TTag tag);

		void EnqueueItemsAndContinueWith<TWorkerContext, TItem, TTag>(TWorkerContext context, ICollection<TItem> items, WorkerAction<TWorkerContext, TItem, TTag> action, ContinueAction<TWorkerContext, TTag> continueAction, TTag tag);
	}
}
