using System.Collections.ObjectModel;
using Mono.Cecil;
using Unity.IL2CPP.AssemblyConversion.Steps.Base;
using Unity.IL2CPP.Attributes;
using Unity.IL2CPP.Contexts;

namespace Unity.IL2CPP.AssemblyConversion.PrimaryCollection.Steps.PerAssembly
{
	public class AttributeSupportCollection : PerAssemblyScheduledStepFunc<GlobalPrimaryCollectionContext, ReadOnlyCollectedAttributeSupportData>
	{
		protected override string Name => "Collecting Attribute Data";

		protected override bool Skip(GlobalPrimaryCollectionContext context)
		{
			return context.Parameters.UsingTinyBackend;
		}

		protected override ReadOnlyCollectedAttributeSupportData ProcessItem(GlobalPrimaryCollectionContext context, AssemblyDefinition item)
		{
			ReadOnlyCollection<AttributeData> attributeData = AttributeSupportCollector.Collect(context.CreateMinimalContext(), item);
			AttributeCollection attributeCollection = AttributeCollection.BuildAttributeCollection(context, attributeData);
			return new ReadOnlyCollectedAttributeSupportData(attributeData, attributeCollection);
		}
	}
}
