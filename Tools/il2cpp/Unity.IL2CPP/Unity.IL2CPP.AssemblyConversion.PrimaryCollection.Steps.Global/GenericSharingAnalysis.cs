using System.Collections.ObjectModel;
using Mono.Cecil;
using Unity.IL2CPP.AssemblyConversion.PrimaryCollection.Results;
using Unity.IL2CPP.AssemblyConversion.Steps.Base;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.GenericSharing;

namespace Unity.IL2CPP.AssemblyConversion.PrimaryCollection.Steps.Global
{
	public class GenericSharingAnalysis : PerAssemblyScheduledStepFuncWithGlobalPostProcessingFunc<GlobalPrimaryCollectionContext, GenericSharingVisitor, GenericSharingAnalysisResults>
	{
		public const string StepName = "Generic Sharing";

		private readonly bool _includeGenerics;

		protected override string Name => "Generic Sharing";

		protected override string PostProcessingSectionName => "Merging Generic Sharing";

		public GenericSharingAnalysis(bool includeGenerics)
		{
			_includeGenerics = includeGenerics;
		}

		protected override bool Skip(GlobalPrimaryCollectionContext context)
		{
			return !_includeGenerics;
		}

		protected override GenericSharingVisitor ProcessItem(GlobalPrimaryCollectionContext context, AssemblyDefinition item)
		{
			GenericSharingVisitor genericSharingVisitor = new GenericSharingVisitor(context.CreateCollectionContext());
			genericSharingVisitor.Collect(item);
			return genericSharingVisitor;
		}

		protected override GenericSharingAnalysisResults CreateEmptyResult()
		{
			return GenericSharingAnalysisResults.Empty;
		}

		protected override GenericSharingAnalysisResults PostProcess(GlobalPrimaryCollectionContext context, ReadOnlyCollection<ResultData<AssemblyDefinition, GenericSharingVisitor>> data)
		{
			GenericSharingVisitor result = data[0].Result;
			for (int i = 1; i < data.Count; i++)
			{
				result.Add(data[i].Result);
			}
			return result.Complete();
		}
	}
}
