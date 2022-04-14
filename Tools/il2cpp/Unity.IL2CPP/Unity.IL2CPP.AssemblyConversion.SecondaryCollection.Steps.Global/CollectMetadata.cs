using Mono.Cecil;
using Unity.IL2CPP.AssemblyConversion.Steps.Base;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Results;
using Unity.IL2CPP.Metadata;

namespace Unity.IL2CPP.AssemblyConversion.SecondaryCollection.Steps.Global
{
	public class CollectMetadata : GlobalScheduledStepFunc<GlobalSecondaryCollectionContext, MetadataCollector, IMetadataCollectionResults>
	{
		protected override string Name => "Collect Metadata";

		protected override bool Skip(GlobalSecondaryCollectionContext context)
		{
			return context.Parameters.UsingTinyBackend;
		}

		protected override void ProcessItem(GlobalSecondaryCollectionContext context, AssemblyDefinition item, MetadataCollector globalState)
		{
			globalState.Add(context.CreateCollectionContext(), item);
		}

		protected override IMetadataCollectionResults CreateEmptyResult()
		{
			return null;
		}

		protected override MetadataCollector CreateGlobalState(GlobalSecondaryCollectionContext context)
		{
			return new MetadataCollector();
		}

		protected override IMetadataCollectionResults GetResults(GlobalSecondaryCollectionContext context, MetadataCollector globalState)
		{
			return globalState.Complete(context.CreateCollectionContext());
		}
	}
}
