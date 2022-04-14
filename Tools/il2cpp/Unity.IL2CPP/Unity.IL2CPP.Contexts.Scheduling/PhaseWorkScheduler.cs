using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts.Forking;
using Unity.IL2CPP.Contexts.Services;
using Unity.MiniProfiling;

namespace Unity.IL2CPP.Contexts.Scheduling
{
	public class PhaseWorkScheduler<TContext> : IPhaseWorkScheduler<TContext>, IWorkScheduler, IDisposable
	{
		private abstract class WorkItem
		{
			public abstract void Invoke(object context, int uniqueId);
		}

		private abstract class WorkItemWithTag<TTag> : WorkItem
		{
			protected readonly TTag _tag;

			protected WorkItemWithTag(TTag tag)
			{
				_tag = tag;
			}
		}

		private sealed class ContextOnlyWorkItem<TWorkerContext> : WorkItemWithTag<object>
		{
			private readonly Action<TWorkerContext> _workerAction;

			public ContextOnlyWorkItem(Action<TWorkerContext> action)
				: base((object)null)
			{
				_workerAction = action;
			}

			public override void Invoke(object context, int uniqueId)
			{
				_workerAction((TWorkerContext)context);
			}
		}

		private sealed class ActionWorkItem<TWorkerContext, TTag> : WorkItemWithTag<TTag>
		{
			private readonly Action<WorkItemData<TWorkerContext, TTag>> _workerAction;

			public ActionWorkItem(Action<WorkItemData<TWorkerContext, TTag>> action, TTag tag)
				: base(tag)
			{
				_workerAction = action;
			}

			public override void Invoke(object context, int uniqueId)
			{
				WorkItemData<TWorkerContext, TTag> obj = new WorkItemData<TWorkerContext, TTag>((TWorkerContext)context, uniqueId, _tag);
				_workerAction(obj);
			}
		}

		private sealed class ActionWorkItem<TWorkerContext> : WorkItem
		{
			private readonly Action<WorkItemData<TWorkerContext>> _workerAction;

			public ActionWorkItem(Action<WorkItemData<TWorkerContext>> action)
			{
				_workerAction = action;
			}

			public override void Invoke(object context, int uniqueId)
			{
				WorkItemData<TWorkerContext> obj = new WorkItemData<TWorkerContext>((TWorkerContext)context, uniqueId);
				_workerAction(obj);
			}
		}

		private sealed class ActionWorkItemWithItem<TWorkerContext, TItem, TTag> : WorkItemWithTag<TTag>
		{
			private readonly WorkerAction<TWorkerContext, TItem, TTag> _workerAction;

			private readonly TItem _item;

			public ActionWorkItemWithItem(WorkerAction<TWorkerContext, TItem, TTag> action, TTag tag, TItem item)
				: base(tag)
			{
				_item = item;
				_workerAction = action;
			}

			public override void Invoke(object context, int uniqueId)
			{
				WorkItemData<TWorkerContext, TItem, TTag> data = new WorkItemData<TWorkerContext, TItem, TTag>((TWorkerContext)context, _item, uniqueId, _tag);
				_workerAction(data);
			}
		}

		private abstract class BaseContinueWorkItem<TItem, TTag> : WorkItemWithTag<TTag>
		{
			public abstract class CollectionSharedData
			{
				private readonly int _expectedTotal;

				private int _completedCount;

				protected CollectionSharedData(int expectedTotal)
				{
					_completedCount = 0;
					_expectedTotal = expectedTotal;
				}

				public void AttemptPostProcess(object context, int uniqueId, TTag tag)
				{
					if (Interlocked.Increment(ref _completedCount) == _expectedTotal)
					{
						PostProcess(context, uniqueId, tag);
					}
				}

				protected abstract void PostProcess(object context, int uniqueId, TTag tag);
			}

			protected readonly TItem _item;

			private readonly CollectionSharedData _shared;

			protected BaseContinueWorkItem(TTag tag, TItem item, CollectionSharedData shared)
				: base(tag)
			{
				_item = item;
				_shared = shared;
			}

			public override void Invoke(object context, int uniqueId)
			{
				try
				{
					InvokeWorker(context, uniqueId);
				}
				finally
				{
					_shared.AttemptPostProcess(context, uniqueId, _tag);
				}
			}

			protected abstract void InvokeWorker(object context, int uniqueId);
		}

		private class ContinueWithWorkItem<TWorkerContext, TItem, TTag> : BaseContinueWorkItem<TItem, TTag>
		{
			public class SharedData : CollectionSharedData
			{
				private readonly ContinueAction<TWorkerContext, TTag> _continueAction;

				public SharedData(int expectedTotal, ContinueAction<TWorkerContext, TTag> continueAction)
					: base(expectedTotal)
				{
					_continueAction = continueAction;
				}

				protected override void PostProcess(object context, int uniqueId, TTag tag)
				{
					WorkItemData<TWorkerContext, TTag> processor = new WorkItemData<TWorkerContext, TTag>((TWorkerContext)context, uniqueId, tag);
					_continueAction(processor);
				}
			}

			private readonly WorkerAction<TWorkerContext, TItem, TTag> _workerAction;

			public ContinueWithWorkItem(WorkerAction<TWorkerContext, TItem, TTag> action, TTag tag, TItem item, SharedData shared)
				: base(tag, item, (CollectionSharedData)shared)
			{
				_workerAction = action;
			}

			protected override void InvokeWorker(object context, int uniqueId)
			{
				WorkItemData<TWorkerContext, TItem, TTag> data = new WorkItemData<TWorkerContext, TItem, TTag>((TWorkerContext)context, _item, uniqueId, _tag);
				_workerAction(data);
			}
		}

		private class ContinueWithResultsWorkItem<TWorkerContext, TItem, TResult, TTag> : BaseContinueWorkItem<TItem, TTag>
		{
			public class SharedData : CollectionSharedData
			{
				public readonly ConcurrentBag<ResultData<TItem, TResult>> Results = new ConcurrentBag<ResultData<TItem, TResult>>();

				private readonly ContinueWithCollectionResults<TWorkerContext, TItem, TResult, TTag> _continueWithResults;

				public SharedData(int expectedTotal, ContinueWithCollectionResults<TWorkerContext, TItem, TResult, TTag> continueWithResults)
					: base(expectedTotal)
				{
					_continueWithResults = continueWithResults;
				}

				protected override void PostProcess(object context, int uniqueId, TTag tag)
				{
					WorkItemData<TWorkerContext, ReadOnlyCollection<ResultData<TItem, TResult>>, TTag> processor = new WorkItemData<TWorkerContext, ReadOnlyCollection<ResultData<TItem, TResult>>, TTag>((TWorkerContext)context, Results.ToArray().AsReadOnly(), uniqueId, tag);
					_continueWithResults(processor);
				}
			}

			private readonly SharedData _shared;

			private readonly WorkerFunc<TWorkerContext, TItem, TResult, TTag> _workerFunc;

			public ContinueWithResultsWorkItem(WorkerFunc<TWorkerContext, TItem, TResult, TTag> workerFunc, TTag tag, TItem item, SharedData shared)
				: base(tag, item, (CollectionSharedData)shared)
			{
				_workerFunc = workerFunc;
				_shared = shared;
			}

			protected override void InvokeWorker(object context, int uniqueId)
			{
				WorkItemData<TWorkerContext, TItem, TTag> data = new WorkItemData<TWorkerContext, TItem, TTag>((TWorkerContext)context, _item, uniqueId, _tag);
				TResult result = _workerFunc(data);
				_shared.Results.Add(new ResultData<TItem, TResult>(_item, result));
			}
		}

		private readonly ReadOnlyCollection<ForkedContextScope<int, TContext>.Data> _contextData;

		private readonly ForkedContextScope<int, TContext> _forkedContextScope;

		private readonly List<Thread> _workerThreads = new List<Thread>();

		private readonly Exception[] _workerThreadDeadExceptions;

		private readonly ConcurrentBag<Exception> _workItemActionExceptions = new ConcurrentBag<Exception>();

		private readonly ManualResetEvent[] _workerIdleFlags;

		private readonly ConcurrentQueue<WorkItem> _workQueue = new ConcurrentQueue<WorkItem>();

		private readonly int _workerCount;

		private readonly Func<TContext, Exception, Exception> _workItemExceptionHandler;

		private readonly SemaphoreSlim _workAvailable = new SemaphoreSlim(0);

		private readonly ManualResetEvent _stop = new ManualResetEvent(initialState: false);

		private bool _disposed;

		private bool _waitComplete;

		public TContext ContextForMainThread => _contextData[_contextData.Count - 1].Context;

		public bool WorkIsDoneOnDifferentThread => true;

		public PhaseWorkScheduler(Func<int, ForkedContextScope<int, TContext>> forker, int workerCount, Func<TContext, Exception, Exception> workerItemExceptionHandler)
		{
			if (workerItemExceptionHandler == null)
			{
				throw new ArgumentNullException("workerItemExceptionHandler");
			}
			_forkedContextScope = forker(workerCount + 1);
			_workerCount = workerCount;
			_contextData = _forkedContextScope.Items;
			_workerThreadDeadExceptions = new Exception[workerCount];
			_workerIdleFlags = new ManualResetEvent[_workerCount];
			_workItemExceptionHandler = workerItemExceptionHandler;
			for (int i = 0; i < _workerCount; i++)
			{
				_workerIdleFlags[i] = new ManualResetEvent(initialState: false);
			}
			Start();
		}

		public void Enqueue<TWorkerContext>(TWorkerContext context, Action<WorkItemData<TWorkerContext>> action)
		{
			EnqueueInternal(new ActionWorkItem<TWorkerContext>(action));
		}

		public void Enqueue<TWorkerContext, TTag>(TWorkerContext context, Action<WorkItemData<TWorkerContext, TTag>> action, TTag tag)
		{
			EnqueueInternal(new ActionWorkItem<TWorkerContext, TTag>(action, tag));
		}

		public void Enqueue<TWorkerContext>(TWorkerContext context, Action<TWorkerContext> action)
		{
			EnqueueInternal(new ContextOnlyWorkItem<TWorkerContext>(action));
		}

		public void Enqueue<TWorkerContext, TItem, TTag>(TWorkerContext context, TItem item, WorkerAction<TWorkerContext, TItem, TTag> action, TTag tag)
		{
			EnqueueInternal(new ActionWorkItemWithItem<TWorkerContext, TItem, TTag>(action, tag, item));
		}

		public void EnqueueItems<TWorkerContext, TItem, TTag>(TWorkerContext context, ICollection<TItem> items, WorkerAction<TWorkerContext, TItem, TTag> action, TTag tag)
		{
			if (items.Count == 0)
			{
				return;
			}
			foreach (TItem item in items)
			{
				_workQueue.Enqueue(new ActionWorkItemWithItem<TWorkerContext, TItem, TTag>(action, tag, item));
			}
			_workAvailable.Release(Math.Min(items.Count, _workerCount));
		}

		public void EnqueueItemsAndContinueWithResults<TWorkerContext, TItem, TResult, TTag>(TWorkerContext context, ICollection<TItem> items, WorkerFunc<TWorkerContext, TItem, TResult, TTag> func, ContinueWithCollectionResults<TWorkerContext, TItem, TResult, TTag> continueWithResults, TTag tag)
		{
			if (items.Count == 0)
			{
				return;
			}
			ContinueWithResultsWorkItem<TWorkerContext, TItem, TResult, TTag>.SharedData shared = new ContinueWithResultsWorkItem<TWorkerContext, TItem, TResult, TTag>.SharedData(items.Count, continueWithResults);
			foreach (TItem item in items)
			{
				_workQueue.Enqueue(new ContinueWithResultsWorkItem<TWorkerContext, TItem, TResult, TTag>(func, tag, item, shared));
			}
			_workAvailable.Release(Math.Min(items.Count, _workerCount));
		}

		public void EnqueueItemsAndContinueWith<TWorkerContext, TItem, TTag>(TWorkerContext context, ICollection<TItem> items, WorkerAction<TWorkerContext, TItem, TTag> action, ContinueAction<TWorkerContext, TTag> continueAction, TTag tag)
		{
			if (items.Count == 0)
			{
				return;
			}
			ContinueWithWorkItem<TWorkerContext, TItem, TTag>.SharedData shared = new ContinueWithWorkItem<TWorkerContext, TItem, TTag>.SharedData(items.Count, continueAction);
			foreach (TItem item in items)
			{
				_workQueue.Enqueue(new ContinueWithWorkItem<TWorkerContext, TItem, TTag>(action, tag, item, shared));
			}
			_workAvailable.Release(Math.Min(items.Count, _workerCount));
		}

		private void EnqueueInternal(WorkItem workItem)
		{
			_workQueue.Enqueue(workItem);
			_workAvailable.Release();
		}

		public void Wait()
		{
			if (_waitComplete)
			{
				return;
			}
			try
			{
				WaitForEmptyQueue();
				JoinThreads();
			}
			finally
			{
				_waitComplete = true;
			}
		}

		public void WaitForEmptyQueue(bool throwExceptions = true)
		{
			using (MiniProfiler.Section("PhaseWorkScheduler.WaitForEmptyQueue"))
			{
				WaitHandle[] waitHandles = _workerIdleFlags.Cast<WaitHandle>().ToArray();
				while (!_workQueue.IsEmpty && !_stop.WaitOne(0))
				{
					WaitHandle.WaitAll(waitHandles);
				}
				if (throwExceptions)
				{
					ThrowExceptions();
				}
			}
		}

		private void JoinThreads()
		{
			using (MiniProfiler.Section("PhaseWorkScheduler.JoinThreads"))
			{
				_stop.Set();
				foreach (Thread workerThread in _workerThreads)
				{
					workerThread.Join();
				}
				ThrowExceptions();
			}
		}

		public void ThrowExceptions()
		{
			Exception[] array = _workerThreadDeadExceptions.Where((Exception e) => e != null).ToArray();
			if (array.Length != 0)
			{
				throw new AggregateException("One or more worker threads hit a fatal exception", array);
			}
			if (_workItemActionExceptions.Count > 0)
			{
				throw new AggregateErrorInformationAlreadyProcessedException("One or more worker items throw an exception", _workItemActionExceptions);
			}
		}

		private void Start()
		{
			for (int i = 0; i < _workerCount; i++)
			{
				Thread thread = new Thread(WorkerLoop);
				thread.Name = "PhaseWorker";
				_workerThreads.Add(thread);
				thread.Start(_contextData[i]);
			}
		}

		private void WorkerLoop(object data)
		{
			ForkedContextScope<int, TContext>.Data data2 = (ForkedContextScope<int, TContext>.Data)data;
			ManualResetEvent manualResetEvent = _workerIdleFlags[data2.Index];
			try
			{
				WaitHandle[] waitHandles = new WaitHandle[2] { _stop, _workAvailable.AvailableWaitHandle };
				while (true)
				{
					int num = 1;
					if (_workQueue.IsEmpty)
					{
						manualResetEvent.Set();
						using (MiniProfiler.Section("Idle"))
						{
							num = WaitHandle.WaitAny(waitHandles);
						}
						manualResetEvent.Reset();
					}
					switch (num)
					{
					case 0:
						return;
					case 1:
					{
						_workAvailable.Wait(0);
						WorkItem result;
						while (_workQueue.TryDequeue(out result))
						{
							try
							{
								result.Invoke(data2.Context, data2.Index);
							}
							catch (Exception ex)
							{
								_stop.Set();
								try
								{
									_workItemActionExceptions.Add(_workItemExceptionHandler(data2.Context, ex));
									return;
								}
								catch (Exception ex2)
								{
									_workItemActionExceptions.Add(new AggregateException(ex, ex2));
									return;
								}
							}
						}
						break;
					}
					default:
						throw new ArgumentException($"Unhandled wait handle index of {num}");
					}
				}
			}
			catch (Exception ex3)
			{
				_workerThreadDeadExceptions[data2.Index] = ex3;
				_stop.Set();
			}
			finally
			{
				manualResetEvent.Set();
			}
		}

		public void Dispose()
		{
			if (!_disposed)
			{
				Wait();
				_forkedContextScope.Dispose();
			}
			ManualResetEvent[] workerIdleFlags = _workerIdleFlags;
			for (int i = 0; i < workerIdleFlags.Length; i++)
			{
				workerIdleFlags[i].Dispose();
			}
			_stop.Dispose();
			_workAvailable.Dispose();
			_disposed = true;
		}
	}
}
