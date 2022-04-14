using System;
using Unity.IL2CPP.Contexts.Forking.Providers;

namespace Unity.IL2CPP.Contexts.Forking.Steps
{
	public class SecondaryWriteDataForker : BaseDataForker<GlobalWriteContext, SecondaryWriteLateAccessForkingContainer>
	{
		private TinyGlobalWriteContext _forkedTinyGlobalWriteContext;

		private GlobalWriteContext _forkedGlobalWriteContext;

		private GlobalMinimalContext _forkedGlobalMinimalContext;

		private GlobalReadOnlyContext _forkedGlobalReadOnlyContext;

		private bool _forkingComplete;

		public SecondaryWriteDataForker(IUnrestrictedContextDataProvider context)
			: base(context)
		{
		}

		protected override SecondaryWriteLateAccessForkingContainer CreateLateAccess()
		{
			return new SecondaryWriteLateAccessForkingContainer(new LateContextAccess<TinyWriteContext>(() => _forkedTinyGlobalWriteContext.CreateWriteContext(), () => _forkingComplete), new LateContextAccess<ReadOnlyContext>(() => _forkedGlobalReadOnlyContext.GetReadOnlyContext(), () => _forkingComplete));
		}

		public override GlobalWriteContext CreateForkedContext()
		{
			_forkedGlobalReadOnlyContext = new GlobalReadOnlyContext(_forkedProvider);
			_forkedGlobalMinimalContext = new GlobalMinimalContext(_forkedProvider, _forkedGlobalReadOnlyContext);
			_forkedGlobalWriteContext = new GlobalWriteContext(_forkedProvider, _forkedProvider, _forkedGlobalMinimalContext, _forkedGlobalReadOnlyContext);
			_forkedTinyGlobalWriteContext = new TinyGlobalWriteContext(_forkedProvider, _forkedGlobalWriteContext, _forkedGlobalMinimalContext, _forkedGlobalReadOnlyContext);
			_forkingComplete = true;
			return _forkedGlobalWriteContext;
		}

		protected override ReadWrite<SecondaryWriteLateAccessForkingContainer, TWrite, TRead, TFull> PickFork<TWrite, TRead, TFull>(IForkableComponent<TWrite, TRead, TFull> component)
		{
			return component.ForkForSecondaryWrite;
		}

		protected override WriteOnly<SecondaryWriteLateAccessForkingContainer, TWrite, TFull> PickFork<TWrite, TFull>(IForkableComponent<TWrite, object, TFull> component)
		{
			return component.ForkForSecondaryWrite;
		}

		protected override ReadOnly<SecondaryWriteLateAccessForkingContainer, TRead, TFull> PickFork<TRead, TFull>(IForkableComponent<object, TRead, TFull> component)
		{
			return component.ForkForSecondaryWrite;
		}

		protected override Action<TFull> PickMerge<TWrite, TRead, TFull>(IForkableComponent<TWrite, TRead, TFull> component)
		{
			return component.MergeForSecondaryWrite;
		}
	}
}
