using System;
using Unity.IL2CPP.Contexts.Forking.Providers;

namespace Unity.IL2CPP.Contexts.Forking.Steps
{
	public class FullPerAssemblyDataForker : PartialPerAssemblyDataForker
	{
		public FullPerAssemblyDataForker(IUnrestrictedContextDataProvider context)
			: base(context)
		{
		}

		protected override ReadWrite<PerAssemblyLateAccessForkingContainer, TWrite, TRead, TFull> PickFork<TWrite, TRead, TFull>(IForkableComponent<TWrite, TRead, TFull> component)
		{
			return component.ForkForFullPerAssembly;
		}

		protected override WriteOnly<PerAssemblyLateAccessForkingContainer, TWrite, TFull> PickFork<TWrite, TFull>(IForkableComponent<TWrite, object, TFull> component)
		{
			return component.ForkForFullPerAssembly;
		}

		protected override ReadOnly<PerAssemblyLateAccessForkingContainer, TRead, TFull> PickFork<TRead, TFull>(IForkableComponent<object, TRead, TFull> component)
		{
			return component.ForkForFullPerAssembly;
		}

		protected override Action<TFull> PickMerge<TWrite, TRead, TFull>(IForkableComponent<TWrite, TRead, TFull> component)
		{
			return component.MergeForFullPerAssembly;
		}
	}
}
