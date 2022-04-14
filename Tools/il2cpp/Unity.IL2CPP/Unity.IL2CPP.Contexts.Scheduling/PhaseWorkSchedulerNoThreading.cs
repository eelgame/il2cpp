using System;
using Unity.IL2CPP.Contexts.Components;
using Unity.IL2CPP.Contexts.Services;

namespace Unity.IL2CPP.Contexts.Scheduling
{
	public class PhaseWorkSchedulerNoThreading<TContext> : ImmediateSchedulerComponent, IPhaseWorkScheduler<TContext>, IWorkScheduler, IDisposable
	{
		private readonly TContext _context;

		public TContext ContextForMainThread => _context;

		public bool WorkIsDoneOnDifferentThread => false;

		public PhaseWorkSchedulerNoThreading(TContext context)
		{
			_context = context;
		}

		public void Dispose()
		{
		}

		public void Wait()
		{
		}

		public void WaitForEmptyQueue(bool throwExceptions = true)
		{
		}

		public void ThrowExceptions()
		{
		}
	}
}
