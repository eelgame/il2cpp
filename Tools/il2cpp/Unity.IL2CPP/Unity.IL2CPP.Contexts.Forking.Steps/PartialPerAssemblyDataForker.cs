using System;
using Unity.IL2CPP.AssemblyConversion;
using Unity.IL2CPP.Contexts.Forking.Providers;

namespace Unity.IL2CPP.Contexts.Forking.Steps
{
	public class PartialPerAssemblyDataForker : BaseDataForker<GlobalFullyForkedContext, PerAssemblyLateAccessForkingContainer>
	{
		private TinyGlobalWriteContext _forkedTinyGlobalWriteContext;

		private GlobalWriteContext _forkedGlobalWriteContext;

		private GlobalMinimalContext _forkedGlobalMinimalContext;

		private GlobalReadOnlyContext _forkedGlobalReadOnlyContext;

		private GlobalPrimaryCollectionContext _forkedGlobalPrimaryCollectionContext;

		private GlobalSecondaryCollectionContext _forkedGlobalSecondaryCollectionContext;

		private GlobalMetadataWriteContext _forkedGlobalMetadataWriteContext;

		private readonly AssemblyConversionResults _phaseResultsContainer;

		private bool _forkingComplete;

		public PartialPerAssemblyDataForker(IUnrestrictedContextDataProvider context)
			: this(context, SetupForkedConversionResultsWithExistingResults(context))
		{
		}

		private PartialPerAssemblyDataForker(IUnrestrictedContextDataProvider context, AssemblyConversionResults phaseResultsContainer)
			: base((ForkedDataProvider)new PerAssemblyForkedDataProvider(context, new ForkedDataContainer(), phaseResultsContainer))
		{
			_phaseResultsContainer = phaseResultsContainer;
		}

		private static AssemblyConversionResults SetupForkedConversionResultsWithExistingResults(IUnrestrictedContextDataProvider context)
		{
			AssemblyConversionResults assemblyConversionResults = new AssemblyConversionResults();
			assemblyConversionResults.SetSetupPhaseResults(context.PhaseResults.Setup);
			return assemblyConversionResults;
		}

		protected override PerAssemblyLateAccessForkingContainer CreateLateAccess()
		{
			return new PerAssemblyLateAccessForkingContainer(new LateContextAccess<TinyWriteContext>(() => _forkedTinyGlobalWriteContext.CreateWriteContext(), () => _forkingComplete), new LateContextAccess<ReadOnlyContext>(() => _forkedGlobalReadOnlyContext.GetReadOnlyContext(), () => _forkingComplete));
		}

		public override GlobalFullyForkedContext CreateForkedContext()
		{
			_forkedGlobalReadOnlyContext = new GlobalReadOnlyContext(_forkedProvider);
			_forkedGlobalMinimalContext = new GlobalMinimalContext(_forkedProvider, _forkedGlobalReadOnlyContext);
			_forkedGlobalWriteContext = new GlobalWriteContext(_forkedProvider, _forkedProvider, _forkedGlobalMinimalContext, _forkedGlobalReadOnlyContext);
			_forkedTinyGlobalWriteContext = new TinyGlobalWriteContext(_forkedProvider, _forkedGlobalWriteContext, _forkedGlobalMinimalContext, _forkedGlobalReadOnlyContext);
			_forkedGlobalPrimaryCollectionContext = new GlobalPrimaryCollectionContext(_forkedProvider, _forkedProvider, _forkedGlobalMinimalContext, _forkedGlobalReadOnlyContext);
			_forkedGlobalSecondaryCollectionContext = new GlobalSecondaryCollectionContext(_forkedProvider, _forkedProvider, _forkedGlobalMinimalContext, _forkedGlobalReadOnlyContext);
			_forkedGlobalMetadataWriteContext = new GlobalMetadataWriteContext(_forkedProvider, _forkedGlobalReadOnlyContext);
			_forkingComplete = true;
			return new GlobalFullyForkedContext(_forkedGlobalReadOnlyContext, _forkedGlobalMinimalContext, _forkedGlobalPrimaryCollectionContext, _forkedGlobalWriteContext, _forkedGlobalSecondaryCollectionContext, _forkedGlobalMetadataWriteContext, _forkedTinyGlobalWriteContext, _phaseResultsContainer, _container);
		}

		protected override ReadWrite<PerAssemblyLateAccessForkingContainer, TWrite, TRead, TFull> PickFork<TWrite, TRead, TFull>(IForkableComponent<TWrite, TRead, TFull> component)
		{
			return component.ForkForPartialPerAssembly;
		}

		protected override WriteOnly<PerAssemblyLateAccessForkingContainer, TWrite, TFull> PickFork<TWrite, TFull>(IForkableComponent<TWrite, object, TFull> component)
		{
			return component.ForkForPartialPerAssembly;
		}

		protected override ReadOnly<PerAssemblyLateAccessForkingContainer, TRead, TFull> PickFork<TRead, TFull>(IForkableComponent<object, TRead, TFull> component)
		{
			return component.ForkForPartialPerAssembly;
		}

		protected override Action<TFull> PickMerge<TWrite, TRead, TFull>(IForkableComponent<TWrite, TRead, TFull> component)
		{
			return component.MergeForPartialPerAssembly;
		}
	}
}
