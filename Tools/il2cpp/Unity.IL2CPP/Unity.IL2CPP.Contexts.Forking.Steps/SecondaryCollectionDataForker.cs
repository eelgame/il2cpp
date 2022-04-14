using System;
using Unity.IL2CPP.Contexts.Forking.Providers;

namespace Unity.IL2CPP.Contexts.Forking.Steps
{
	public class SecondaryCollectionDataForker : BaseDataForker<GlobalSecondaryCollectionContext, SecondaryCollectionLateAccessForkingContainer>
	{
		private GlobalSecondaryCollectionContext _forkedGlobalCollectionContext;

		private GlobalMinimalContext _forkedGlobalMinimalContext;

		private GlobalReadOnlyContext _forkedGlobalReadOnlyContext;

		private bool _forkingComplete;

		public SecondaryCollectionDataForker(IUnrestrictedContextDataProvider context)
			: base(context)
		{
		}

		protected override SecondaryCollectionLateAccessForkingContainer CreateLateAccess()
		{
			return new SecondaryCollectionLateAccessForkingContainer(new LateContextAccess<ReadOnlyContext>(() => _forkedGlobalReadOnlyContext.GetReadOnlyContext(), () => _forkingComplete));
		}

		public override GlobalSecondaryCollectionContext CreateForkedContext()
		{
			_forkedGlobalReadOnlyContext = new GlobalReadOnlyContext(_forkedProvider);
			_forkedGlobalMinimalContext = new GlobalMinimalContext(_forkedProvider, _forkedGlobalReadOnlyContext);
			_forkedGlobalCollectionContext = new GlobalSecondaryCollectionContext(_forkedProvider, _forkedProvider, _forkedGlobalMinimalContext, _forkedGlobalReadOnlyContext);
			_forkingComplete = true;
			return _forkedGlobalCollectionContext;
		}

		protected override ReadWrite<SecondaryCollectionLateAccessForkingContainer, TWrite, TRead, TFull> PickFork<TWrite, TRead, TFull>(IForkableComponent<TWrite, TRead, TFull> component)
		{
			return component.ForkForSecondaryCollection;
		}

		protected override WriteOnly<SecondaryCollectionLateAccessForkingContainer, TWrite, TFull> PickFork<TWrite, TFull>(IForkableComponent<TWrite, object, TFull> component)
		{
			return component.ForkForSecondaryCollection;
		}

		protected override ReadOnly<SecondaryCollectionLateAccessForkingContainer, TRead, TFull> PickFork<TRead, TFull>(IForkableComponent<object, TRead, TFull> component)
		{
			return component.ForkForSecondaryCollection;
		}

		protected override Action<TFull> PickMerge<TWrite, TRead, TFull>(IForkableComponent<TWrite, TRead, TFull> component)
		{
			return component.MergeForSecondaryCollection;
		}
	}
}
