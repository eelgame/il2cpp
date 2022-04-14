using System;
using Unity.IL2CPP.Contexts.Forking.Steps;

namespace Unity.IL2CPP.Contexts.Components.Base
{
	public abstract class StatelessComponentBase<TWrite, TRead, TFull> : ComponentBase<TWrite, TRead, TFull> where TFull : ComponentBase<TWrite, TRead, TFull>, TWrite, TRead
	{
		protected void WriteOnlyFork(out TWrite writer, out TRead reader, out TFull full)
		{
			base.WriteOnlyFork(out writer, out reader, out full, ForkMode.ReuseThis, MergeMode.None);
		}

		protected override TFull CreateEmptyInstance()
		{
			throw new NotSupportedException();
		}

		protected override TFull CreateCopyInstance()
		{
			throw new NotSupportedException();
		}

		protected override void HandleMergeForAdd(TFull forked)
		{
			throw new NotSupportedException($"A stateless component should never use {MergeMode.Add} because there should never have been anything added to merge");
		}

		protected override void HandleMergeForMergeValues(TFull forked)
		{
			throw new NotSupportedException($"A stateless component should never use {MergeMode.MergeValues} because there should never have been anything added to merge");
		}

		protected override void ForkForPartialPerAssembly(PerAssemblyLateAccessForkingContainer lateAccess, out TWrite writer, out TRead reader, out TFull full)
		{
			WriteOnlyFork(out writer, out reader, out full);
		}

		protected override void ForkForFullPerAssembly(PerAssemblyLateAccessForkingContainer lateAccess, out TWrite writer, out TRead reader, out TFull full)
		{
			WriteOnlyFork(out writer, out reader, out full);
		}
	}
}
