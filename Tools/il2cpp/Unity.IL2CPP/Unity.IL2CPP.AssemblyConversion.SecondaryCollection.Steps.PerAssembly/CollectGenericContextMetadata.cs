using Mono.Cecil;
using Unity.IL2CPP.AssemblyConversion.Steps.Base;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Metadata;

namespace Unity.IL2CPP.AssemblyConversion.SecondaryCollection.Steps.PerAssembly
{
	public class CollectGenericContextMetadata : PerAssemblyScheduledStepFunc<GlobalSecondaryCollectionContext, GenericContextCollection>
	{
		protected override string Name => "Collect Generic Context Metadata";

		protected override bool Skip(GlobalSecondaryCollectionContext context)
		{
			return context.Parameters.UsingTinyBackend;
		}

		protected override GenericContextCollection ProcessItem(GlobalSecondaryCollectionContext context, AssemblyDefinition item)
		{
			return GenericContextCollector.Collect(context.CreateCollectionContext(), item, context.Results.PrimaryCollection.GenericSharingAnalysis);
		}
	}
}
