using Mono.Cecil;
using Unity.IL2CPP.AssemblyConversion.PrimaryCollection.Results;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Scheduling;
using Unity.MiniProfiling;

namespace Unity.IL2CPP.AssemblyConversion.InProcessPerAssembly
{
	public abstract class BaseConversionContainer
	{
		public abstract string Name { get; }

		public abstract string CleanName { get; }

		public int Index { get; }

		protected BaseConversionContainer(int index)
		{
			Index = index;
		}

		public abstract bool IncludeTypeDefinitionInContext(TypeReference type);

		public void RunPrimaryCollection(GlobalFullyForkedContext context, GenericSharingAnalysisResults genericSharingAnalysisResults)
		{
			using (MiniProfiler.Section("PrimaryCollectionPhase", Name))
			{
				AssemblyConversionResults.PrimaryCollectionPhase primaryCollectionResults = PrimaryCollectionPhase(context, genericSharingAnalysisResults);
				context.Results.SetPrimaryCollectionResults(primaryCollectionResults);
			}
		}

		public void RunPrimaryWrite(GlobalFullyForkedContext context)
		{
			using (MiniProfiler.Section("PrimaryWritePhase", Name))
			{
				AssemblyConversionResults.PrimaryWritePhase primaryWritePhaseResults = PrimaryWritePhase(context);
				context.Results.SetPrimaryWritePhaseResults(primaryWritePhaseResults);
			}
		}

		public void RunSecondaryCollection(GlobalFullyForkedContext context)
		{
			using (MiniProfiler.Section("SecondaryCollectionPhase", Name))
			{
				AssemblyConversionResults.SecondaryCollectionPhase secondaryCollectionPhaseResults = SecondaryCollectionPhase(context);
				context.Results.SetSecondaryCollectionPhaseResults(secondaryCollectionPhaseResults);
			}
		}

		public void RunSecondaryWrite(GlobalFullyForkedContext context)
		{
			using (MiniProfiler.Section("SecondaryWritePhase", Name))
			{
				AssemblyConversionResults.SecondaryWritePhasePart1 secondaryWritePhasePart1Results = SecondaryWritePhasePart1(context);
				context.Results.SetSecondaryWritePhasePart1Results(secondaryWritePhasePart1Results);
				AssemblyConversionResults.SecondaryWritePhasePart3 secondaryWritePhasePart3Results = SecondaryWritePhasePart3(context);
				context.Results.SetSecondaryWritePhasePart3Results(secondaryWritePhasePart3Results);
				AssemblyConversionResults.SecondaryWritePhase secondaryWritePhaseResults = SecondaryWritePhasePart4(context);
				context.Results.SetSecondaryWritePhaseResults(secondaryWritePhaseResults);
			}
		}

		public void RunMetadataWrite(GlobalFullyForkedContext context)
		{
			using (MiniProfiler.Section("MetadataWritePhase", Name))
			{
				MetadataWritePhase(context);
			}
		}

		public void RunCompletion(GlobalFullyForkedContext context)
		{
			using (MiniProfiler.Section("CompletionPhase", Name))
			{
				CompletionPhase(context);
			}
		}

		protected abstract AssemblyConversionResults.PrimaryCollectionPhase PrimaryCollectionPhase(GlobalFullyForkedContext context, GenericSharingAnalysisResults genericSharingAnalysisResults);

		protected abstract AssemblyConversionResults.PrimaryWritePhase PrimaryWritePhase(GlobalFullyForkedContext context);

		protected abstract AssemblyConversionResults.SecondaryCollectionPhase SecondaryCollectionPhase(GlobalFullyForkedContext context);

		protected abstract AssemblyConversionResults.SecondaryWritePhasePart1 SecondaryWritePhasePart1(GlobalFullyForkedContext context);

		protected abstract AssemblyConversionResults.SecondaryWritePhasePart3 SecondaryWritePhasePart3(GlobalFullyForkedContext context);

		protected abstract AssemblyConversionResults.SecondaryWritePhase SecondaryWritePhasePart4(GlobalFullyForkedContext context);

		protected abstract void MetadataWritePhase(GlobalFullyForkedContext context);

		protected abstract void CompletionPhase(GlobalFullyForkedContext context);

		protected IPhaseWorkScheduler<TContext> CreateHackedScheduler<TContext>(TContext context)
		{
			return new PhaseWorkSchedulerNoThreading<TContext>(context);
		}
	}
}
