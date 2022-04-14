using System;
using Unity.IL2CPP.Contexts.Forking.Providers;

namespace Unity.IL2CPP.Contexts.Forking.Steps
{
	public class PrimaryCollectionDataForker : BaseDataForker<GlobalPrimaryCollectionContext, PrimaryCollectionLateAccessForkingContainer>
	{
		private GlobalPrimaryCollectionContext _forkedGlobalCollectionContext;

		private GlobalMinimalContext _forkedGlobalMinimalContext;

		private GlobalReadOnlyContext _forkedGlobalReadOnlyContext;

		private bool _forkingComplete;

		public PrimaryCollectionDataForker(IUnrestrictedContextDataProvider context)
			: base(context)
		{
		}

		protected override PrimaryCollectionLateAccessForkingContainer CreateLateAccess()
		{
			return new PrimaryCollectionLateAccessForkingContainer(new LateContextAccess<ReadOnlyContext>(() => _forkedGlobalReadOnlyContext.GetReadOnlyContext(), () => _forkingComplete));
		}

		public override GlobalPrimaryCollectionContext CreateForkedContext()
		{
			_forkedGlobalReadOnlyContext = new GlobalReadOnlyContext(_forkedProvider);
			_forkedGlobalMinimalContext = new GlobalMinimalContext(_forkedProvider, _forkedGlobalReadOnlyContext);
			_forkedGlobalCollectionContext = new GlobalPrimaryCollectionContext(_forkedProvider, _forkedProvider, _forkedGlobalMinimalContext, _forkedGlobalReadOnlyContext);
			_forkingComplete = true;
			return _forkedGlobalCollectionContext;
		}

		protected override ReadWrite<PrimaryCollectionLateAccessForkingContainer, TWrite, TRead, TFull> PickFork<TWrite, TRead, TFull>(IForkableComponent<TWrite, TRead, TFull> component)
		{
			return component.ForkForPrimaryCollection;
		}

		protected override WriteOnly<PrimaryCollectionLateAccessForkingContainer, TWrite, TFull> PickFork<TWrite, TFull>(IForkableComponent<TWrite, object, TFull> component)
		{
			return component.ForkForPrimaryCollection;
		}

		protected override ReadOnly<PrimaryCollectionLateAccessForkingContainer, TRead, TFull> PickFork<TRead, TFull>(IForkableComponent<object, TRead, TFull> component)
		{
			return component.ForkForPrimaryCollection;
		}

		protected override Action<TFull> PickMerge<TWrite, TRead, TFull>(IForkableComponent<TWrite, TRead, TFull> component)
		{
			return component.MergeForPrimaryCollection;
		}
	}
}
