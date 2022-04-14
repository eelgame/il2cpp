using System;
using Unity.IL2CPP.Contexts.Forking.Steps;

namespace Unity.IL2CPP.Contexts.Components.Base
{
	public abstract class ServiceComponentBase<TRead, TFull> : ComponentBase<object, TRead, TFull> where TFull : ComponentBase<object, TRead, TFull>, TRead
	{
		protected override void HandleMergeForAdd(TFull forked)
		{
			throw new NotSupportedException("A service component should never have data to merge");
		}

		protected override void HandleMergeForMergeValues(TFull forked)
		{
			throw new NotSupportedException("A service component should never have data to merge");
		}

		protected override TFull CreateEmptyInstance()
		{
			throw new NotSupportedException("A service component never needs a new instance create");
		}

		protected override TFull CreateCopyInstance()
		{
			throw new NotSupportedException("A service component never needs to be copied");
		}

		protected override object GetNotAvailableWrite()
		{
			throw new NotSupportedException("A service component has no write interface");
		}

		protected override TRead GetNotAvailableRead()
		{
			throw new NotSupportedException("A service component is always available");
		}

		protected override void ForkForSecondaryCollection(SecondaryCollectionLateAccessForkingContainer lateAccess, out object writer, out TRead reader, out TFull full)
		{
			ReadOnlyFork(out writer, out reader, out full);
		}

		protected override void ForkForPrimaryWrite(PrimaryWriteAssembliesLateAccessForkingContainer lateAccess, out object writer, out TRead reader, out TFull full)
		{
			ReadOnlyFork(out writer, out reader, out full);
		}

		protected override void ForkForPrimaryCollection(PrimaryCollectionLateAccessForkingContainer lateAccess, out object writer, out TRead reader, out TFull full)
		{
			ReadOnlyFork(out writer, out reader, out full);
		}

		protected override void ForkForSecondaryWrite(SecondaryWriteLateAccessForkingContainer lateAccess, out object writer, out TRead reader, out TFull full)
		{
			ReadOnlyFork(out writer, out reader, out full);
		}

		protected override void ForkForPartialPerAssembly(PerAssemblyLateAccessForkingContainer lateAccess, out object writer, out TRead reader, out TFull full)
		{
			ReadOnlyFork(out writer, out reader, out full);
		}

		protected override void ForkForFullPerAssembly(PerAssemblyLateAccessForkingContainer lateAccess, out object writer, out TRead reader, out TFull full)
		{
			ReadOnlyFork(out writer, out reader, out full);
		}
	}
}
