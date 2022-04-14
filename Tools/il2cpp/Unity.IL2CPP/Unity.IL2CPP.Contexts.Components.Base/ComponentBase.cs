using System;
using Unity.IL2CPP.Contexts.Forking;
using Unity.IL2CPP.Contexts.Forking.Steps;

namespace Unity.IL2CPP.Contexts.Components.Base
{
	public abstract class ComponentBase<TWrite, TRead, TFull> : IForkableComponent<TWrite, TRead, TFull> where TFull : ComponentBase<TWrite, TRead, TFull>, TWrite, TRead
	{
		public enum ForkMode
		{
			Empty,
			Copy,
			ReuseThis
		}

		public enum MergeMode
		{
			None,
			Add,
			MergeValues
		}

		private MergeMode _mergeMode;

		protected ComponentBase()
		{
			_mergeMode = MergeMode.None;
		}

		public virtual void ReadOnlyFork(LateAccessForkingContainer lateAccess, out TWrite writer, out TRead reader, out TFull full, ForkMode forkMode, MergeMode mergeMode = MergeMode.None)
		{
			writer = ((typeof(TWrite) == typeof(object)) ? default(TWrite) : GetNotAvailableWrite());
			reader = (TRead)(full = SetupForkedInstance(lateAccess, forkMode, mergeMode));
		}

		public virtual void ReadOnlyFork(out TWrite writer, out TRead reader, out TFull full, ForkMode forkMode = ForkMode.ReuseThis)
		{
			ReadOnlyForkInternal(out writer, out reader, out full, forkMode, MergeMode.None);
		}

		public virtual void ReadOnlyForkWithMergeAbility(out TWrite writer, out TRead reader, out TFull full, ForkMode forkMode = ForkMode.ReuseThis, MergeMode mergeMode = MergeMode.None)
		{
			ReadOnlyForkInternal(out writer, out reader, out full, forkMode, mergeMode);
		}

		private void ReadOnlyForkInternal(out TWrite writer, out TRead reader, out TFull full, ForkMode forkMode, MergeMode mergeMode)
		{
			writer = ((typeof(TWrite) == typeof(object)) ? default(TWrite) : GetNotAvailableWrite());
			reader = (TRead)(full = SetupForkedInstance(null, forkMode, mergeMode));
		}

		public virtual void WriteOnlyFork(out TWrite writer, out TRead reader, out TFull full, ForkMode forkMode = ForkMode.Empty, MergeMode mergeMode = MergeMode.Add)
		{
			writer = (TWrite)(full = SetupForkedInstance(null, forkMode, mergeMode));
			reader = ((typeof(TRead) == typeof(object)) ? default(TRead) : GetNotAvailableRead());
		}

		protected virtual void NotAvailableFork(out TWrite writer, out TRead reader, out TFull full, ForkMode forkMode = ForkMode.Empty)
		{
			full = SetupForkedInstance(null, forkMode, MergeMode.None);
			writer = ((typeof(TWrite) == typeof(object)) ? default(TWrite) : GetNotAvailableWrite());
			reader = ((typeof(TRead) == typeof(object)) ? default(TRead) : GetNotAvailableRead());
		}

		public virtual void ReadWriteFork(out TWrite writer, out TRead reader, out TFull full, ForkMode mode = ForkMode.Copy, MergeMode mergeMode = MergeMode.Add)
		{
			full = SetupForkedInstance(null, mode, mergeMode);
			writer = (TWrite)full;
			reader = (TRead)full;
		}

		private TFull SetupForkedInstance(LateAccessForkingContainer lateAccess, ForkMode forkMode, MergeMode mergeMode)
		{
			if (lateAccess != null && forkMode == ForkMode.ReuseThis)
			{
				throw new ArgumentException($"{ForkMode.ReuseThis} cannot be used when a late access container is needed.  A new instance must be created that uses the supplied late access");
			}
			TFull val;
			switch (forkMode)
			{
			case ForkMode.Empty:
				val = ((lateAccess == null) ? CreateEmptyInstance() : CreateEmptyInstance(lateAccess));
				break;
			case ForkMode.Copy:
				val = ((lateAccess == null) ? CreateCopyInstance() : CreateCopyInstance(lateAccess));
				break;
			case ForkMode.ReuseThis:
				val = ThisAsFull();
				break;
			default:
				throw new ArgumentException(string.Format("Unhandled {0} value of `{1}`", "ForkMode", forkMode));
			}
			if (forkMode == ForkMode.ReuseThis && _mergeMode != mergeMode)
			{
				throw new ArgumentException($"Cannot use {ForkMode.ReuseThis} when the merge mode ({mergeMode}) differs from the parent({_mergeMode}).  This can cause issues tracking the merge state when nested forking occurs because we overwrite the parents merge mode.");
			}
			val._mergeMode = mergeMode;
			return val;
		}

		protected void Merge(TFull forked)
		{
			try
			{
				switch (forked._mergeMode)
				{
				case MergeMode.None:
					break;
				case MergeMode.Add:
					HandleMergeForAdd(forked);
					break;
				case MergeMode.MergeValues:
					HandleMergeForMergeValues(forked);
					break;
				default:
					throw new ArgumentException($"Unhandled merge mode {forked._mergeMode}");
				}
			}
			catch (Exception innerException)
			{
				throw new InvalidOperationException($"Exception while merging {GetType()}", innerException);
			}
		}

		protected abstract void HandleMergeForAdd(TFull forked);

		protected abstract void HandleMergeForMergeValues(TFull forked);

		protected abstract TFull CreateEmptyInstance();

		protected abstract TFull CreateCopyInstance();

		protected virtual TFull CreateEmptyInstance(LateAccessForkingContainer lateAccess)
		{
			throw new NotSupportedException("Components that need access to the late access forking container must override this method");
		}

		protected virtual TFull CreateCopyInstance(LateAccessForkingContainer lateAccess)
		{
			throw new NotSupportedException("Components that need access to the late access forking container must override this method");
		}

		protected abstract TFull ThisAsFull();

		protected abstract TRead ThisAsRead();

		protected abstract TWrite GetNotAvailableWrite();

		protected abstract TRead GetNotAvailableRead();

		protected abstract void ForkForPrimaryWrite(PrimaryWriteAssembliesLateAccessForkingContainer lateAccess, out TWrite writer, out TRead reader, out TFull full);

		protected abstract void ForkForPrimaryCollection(PrimaryCollectionLateAccessForkingContainer lateAccess, out TWrite writer, out TRead reader, out TFull full);

		protected abstract void ForkForSecondaryWrite(SecondaryWriteLateAccessForkingContainer lateAccess, out TWrite writer, out TRead reader, out TFull full);

		protected abstract void ForkForSecondaryCollection(SecondaryCollectionLateAccessForkingContainer lateAccess, out TWrite writer, out TRead reader, out TFull full);

		protected virtual void ForkForPartialPerAssembly(PerAssemblyLateAccessForkingContainer lateAccess, out TWrite writer, out TRead reader, out TFull full)
		{
			ReadWriteFork(out writer, out reader, out full, ForkMode.Empty);
		}

		protected virtual void ForkForFullPerAssembly(PerAssemblyLateAccessForkingContainer lateAccess, out TWrite writer, out TRead reader, out TFull full)
		{
			ReadWriteFork(out writer, out reader, out full, ForkMode.Empty, MergeMode.None);
		}

		void IForkableComponent<TWrite, TRead, TFull>.ForkForPrimaryWrite(PrimaryWriteAssembliesLateAccessForkingContainer lateAccess, out TWrite writer, out TRead reader, out TFull full)
		{
			ForkForPrimaryWrite(lateAccess, out writer, out reader, out full);
		}

		void IForkableComponent<TWrite, TRead, TFull>.MergeForPrimaryWrite(TFull forked)
		{
			Merge(forked);
		}

		void IForkableComponent<TWrite, TRead, TFull>.ForkForPrimaryCollection(PrimaryCollectionLateAccessForkingContainer lateAccess, out TWrite writer, out TRead reader, out TFull full)
		{
			ForkForPrimaryCollection(lateAccess, out writer, out reader, out full);
		}

		void IForkableComponent<TWrite, TRead, TFull>.MergeForPrimaryCollection(TFull forked)
		{
			Merge(forked);
		}

		void IForkableComponent<TWrite, TRead, TFull>.ForkForSecondaryWrite(SecondaryWriteLateAccessForkingContainer lateAccess, out TWrite writer, out TRead reader, out TFull full)
		{
			ForkForSecondaryWrite(lateAccess, out writer, out reader, out full);
		}

		void IForkableComponent<TWrite, TRead, TFull>.MergeForSecondaryWrite(TFull forked)
		{
			Merge(forked);
		}

		void IForkableComponent<TWrite, TRead, TFull>.ForkForSecondaryCollection(SecondaryCollectionLateAccessForkingContainer lateAccess, out TWrite writer, out TRead reader, out TFull full)
		{
			ForkForSecondaryCollection(lateAccess, out writer, out reader, out full);
		}

		void IForkableComponent<TWrite, TRead, TFull>.MergeForSecondaryCollection(TFull forked)
		{
			Merge(forked);
		}

		void IForkableComponent<TWrite, TRead, TFull>.ForkForPartialPerAssembly(PerAssemblyLateAccessForkingContainer lateAccess, out TWrite writer, out TRead reader, out TFull full)
		{
			ForkForPartialPerAssembly(lateAccess, out writer, out reader, out full);
		}

		void IForkableComponent<TWrite, TRead, TFull>.MergeForPartialPerAssembly(TFull forked)
		{
			Merge(forked);
		}

		void IForkableComponent<TWrite, TRead, TFull>.ForkForFullPerAssembly(PerAssemblyLateAccessForkingContainer lateAccess, out TWrite writer, out TRead reader, out TFull full)
		{
			ForkForFullPerAssembly(lateAccess, out writer, out reader, out full);
		}

		void IForkableComponent<TWrite, TRead, TFull>.MergeForFullPerAssembly(TFull forked)
		{
			Merge(forked);
		}
	}
}
