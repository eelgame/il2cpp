using System.Collections.ObjectModel;
using Mono.Cecil;
using Unity.Cecil.Visitor;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.GenericsCollection.CodeFlow;

namespace Unity.IL2CPP.GenericsCollection
{
	public static class GenericsCollector
	{
		public static InflatedCollectionCollector Collect(PrimaryCollectionContext context, ReadOnlyCollection<AssemblyDefinition> assemblies)
		{
			InflatedCollectionCollector inflatedCollectionCollector = GenericCodeFlowGraphCollector.Collect(context, assemblies);
			foreach (AssemblyDefinition assembly in assemblies)
			{
				GenericContextFreeVisitor visitor = new GenericContextFreeVisitor(context, inflatedCollectionCollector);
				assembly.Accept(visitor);
			}
			return inflatedCollectionCollector;
		}

		public static InflatedCollectionCollector Collect(PrimaryCollectionContext context, TypeDefinition type)
		{
			InflatedCollectionCollector inflatedCollectionCollector = new InflatedCollectionCollector();
			GenericContextFreeVisitor visitor = new GenericContextFreeVisitor(context, inflatedCollectionCollector);
			type.Accept(visitor);
			return inflatedCollectionCollector;
		}
	}
}
