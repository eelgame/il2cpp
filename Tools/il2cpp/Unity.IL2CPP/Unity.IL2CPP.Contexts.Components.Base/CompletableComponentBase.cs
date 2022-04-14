using System;
using Unity.IL2CPP.Contexts.Forking.Steps;

namespace Unity.IL2CPP.Contexts.Components.Base
{
	public abstract class CompletableComponentBase<TComplete, TWrite, TFull> : ComponentBase<TWrite, object, TFull> where TFull : CompletableComponentBase<TComplete, TWrite, TFull>, TWrite
	{
		private bool _complete;

		public virtual TComplete Complete()
		{
			AssertNotComplete();
			_complete = true;
			return GetResults();
		}

		protected abstract TComplete GetResults();

		protected void SetComplete()
		{
			_complete = true;
		}

		protected override object ThisAsRead()
		{
			throw new NotSupportedException();
		}

		protected override object GetNotAvailableRead()
		{
			throw new NotSupportedException();
		}

		public override void ReadWriteFork(out TWrite writer, out object reader, out TFull full, ForkMode mode = ForkMode.Copy, MergeMode mergeMode = MergeMode.Add)
		{
			throw new NotSupportedException("Completable components have no read interface");
		}

		public override void WriteOnlyFork(out TWrite writer, out object reader, out TFull full, ForkMode forkMode = ForkMode.Empty, MergeMode mergeMode = MergeMode.Add)
		{
			AssertNotComplete();
			base.WriteOnlyFork(out writer, out reader, out full, forkMode, mergeMode);
		}

		public override void ReadOnlyFork(out TWrite writer, out object reader, out TFull full, ForkMode forkMode = ForkMode.ReuseThis)
		{
			throw new NotSupportedException("Completable components have no read interface");
		}

		public override void ReadOnlyForkWithMergeAbility(out TWrite writer, out object reader, out TFull full, ForkMode forkMode = ForkMode.ReuseThis, MergeMode mergeMode = MergeMode.None)
		{
			throw new NotSupportedException("Completable components have no read interface");
		}

		protected override void ForkForPartialPerAssembly(PerAssemblyLateAccessForkingContainer lateAccess, out TWrite writer, out object reader, out TFull full)
		{
			WriteOnlyFork(out writer, out reader, out full);
		}

		protected override void ForkForFullPerAssembly(PerAssemblyLateAccessForkingContainer lateAccess, out TWrite writer, out object reader, out TFull full)
		{
			((ComponentBase<TWrite, object, TFull>)this).WriteOnlyFork(out writer, out reader, out full, ForkMode.Empty, MergeMode.None);
		}

		protected void AssertNotComplete()
		{
			if (_complete)
			{
				throw new InvalidOperationException("Once Complete() has been called, items cannot be added");
			}
		}
	}
}
