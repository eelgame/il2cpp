using System;
using Unity.IL2CPP.Contexts.Forking;
using Unity.IL2CPP.Contexts.Forking.Steps;

namespace Unity.IL2CPP.Contexts.Components.Base
{
	public abstract class MayViolateContextBoundariesComponentBase<TContext, TRead, TFull> : ServiceComponentBase<TRead, TFull> where TContext : class where TFull : ServiceComponentBase<TRead, TFull>, TRead
	{
		private readonly LateContextAccess<TContext> _contextAccess;

		protected TContext Context => _contextAccess.Context;

		protected MayViolateContextBoundariesComponentBase(LateContextAccess<TContext> contextAccess)
		{
			_contextAccess = contextAccess;
		}

		protected override TFull ThisAsFull()
		{
			throw new NotSupportedException("This can never be reused because the late access context is not safe to reuse");
		}

		protected override TRead ThisAsRead()
		{
			throw new NotSupportedException("This can never be reused because the late access context is not safe to reuse");
		}

		protected override TFull CreateCopyInstance()
		{
			throw new NotSupportedException("The overload with the late access container must be used");
		}

		protected override TFull CreateEmptyInstance()
		{
			throw new NotSupportedException("The overload with the late access container must be used");
		}

		public override void ReadOnlyFork(out object writer, out TRead reader, out TFull full, ForkMode forkMode = ForkMode.ReuseThis)
		{
			throw new NotSupportedException("The overload with the late access container must be used");
		}

		public override void ReadOnlyForkWithMergeAbility(out object writer, out TRead reader, out TFull full, ForkMode forkMode = ForkMode.ReuseThis, MergeMode mergeMode = MergeMode.None)
		{
			throw new NotSupportedException("The overload with the late access container must be used");
		}

		public override void ReadWriteFork(out object writer, out TRead reader, out TFull full, ForkMode mode = ForkMode.Copy, MergeMode mergeMode = MergeMode.Add)
		{
			throw new NotSupportedException("The overload with the late access container must be used");
		}

		public override void WriteOnlyFork(out object writer, out TRead reader, out TFull full, ForkMode forkMode = ForkMode.Empty, MergeMode mergeMode = MergeMode.Add)
		{
			throw new NotSupportedException("The overload with the late access container must be used");
		}

		protected override void ForkForSecondaryCollection(SecondaryCollectionLateAccessForkingContainer lateAccess, out object writer, out TRead reader, out TFull full)
		{
			((ComponentBase<object, TRead, TFull>)this).ReadOnlyFork((LateAccessForkingContainer)lateAccess, out writer, out reader, out full, ForkMode.Empty, MergeMode.None);
		}

		protected override void ForkForPrimaryWrite(PrimaryWriteAssembliesLateAccessForkingContainer lateAccess, out object writer, out TRead reader, out TFull full)
		{
			((ComponentBase<object, TRead, TFull>)this).ReadOnlyFork((LateAccessForkingContainer)lateAccess, out writer, out reader, out full, ForkMode.Empty, MergeMode.None);
		}

		protected override void ForkForPrimaryCollection(PrimaryCollectionLateAccessForkingContainer lateAccess, out object writer, out TRead reader, out TFull full)
		{
			((ComponentBase<object, TRead, TFull>)this).ReadOnlyFork((LateAccessForkingContainer)lateAccess, out writer, out reader, out full, ForkMode.Empty, MergeMode.None);
		}

		protected override void ForkForSecondaryWrite(SecondaryWriteLateAccessForkingContainer lateAccess, out object writer, out TRead reader, out TFull full)
		{
			((ComponentBase<object, TRead, TFull>)this).ReadOnlyFork((LateAccessForkingContainer)lateAccess, out writer, out reader, out full, ForkMode.Empty, MergeMode.None);
		}

		protected override void ForkForPartialPerAssembly(PerAssemblyLateAccessForkingContainer lateAccess, out object writer, out TRead reader, out TFull full)
		{
			((ComponentBase<object, TRead, TFull>)this).ReadOnlyFork((LateAccessForkingContainer)lateAccess, out writer, out reader, out full, ForkMode.Empty, MergeMode.None);
		}

		protected override void ForkForFullPerAssembly(PerAssemblyLateAccessForkingContainer lateAccess, out object writer, out TRead reader, out TFull full)
		{
			((ComponentBase<object, TRead, TFull>)this).ReadOnlyFork((LateAccessForkingContainer)lateAccess, out writer, out reader, out full, ForkMode.Empty, MergeMode.None);
		}
	}
}
