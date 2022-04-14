using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Mono.Cecil;
using Unity.Cecil.Awesome.Ordering;

namespace Unity.IL2CPP.GenericsCollection
{
	public class ReadOnlyInflatedCollectionCollector
	{
		public readonly ReadOnlyCollection<GenericInstanceType> Types;

		public readonly ReadOnlyCollection<GenericInstanceType> TypeDeclarations;

		public readonly ReadOnlyCollection<TypeReference> InstantiatedGenericsAndArrays;

		public readonly ReadOnlyCollection<GenericInstanceMethod> Methods;

		public static ReadOnlyInflatedCollectionCollector Empty => new ReadOnlyInflatedCollectionCollector(new InflatedCollectionCollector());

		public ReadOnlyInflatedCollectionCollector(InflatedCollectionCollector inflatedCollectionCollector)
		{
			Types = inflatedCollectionCollector.Types.Items.ToSortedCollection();
			TypeDeclarations = inflatedCollectionCollector.TypeDeclarations.Items.ToSortedCollection();
			InstantiatedGenericsAndArrays = inflatedCollectionCollector.InstantiatedGenericsAndArrays.Items.ToSortedCollection();
			Methods = inflatedCollectionCollector.Methods.Items.ToSortedCollection();
		}

		private ReadOnlyInflatedCollectionCollector(IEnumerable<ReadOnlyInflatedCollectionCollector> others)
		{
			Types = others.Aggregate(new HashSet<GenericInstanceType>(), delegate(HashSet<GenericInstanceType> accum, ReadOnlyInflatedCollectionCollector item)
			{
				accum.UnionWith(item.Types);
				return accum;
			}).ToSortedCollection();
			TypeDeclarations = others.Aggregate(new HashSet<GenericInstanceType>(), delegate(HashSet<GenericInstanceType> accum, ReadOnlyInflatedCollectionCollector item)
			{
				accum.UnionWith(item.TypeDeclarations);
				return accum;
			}).ToSortedCollection();
			InstantiatedGenericsAndArrays = others.Aggregate(new HashSet<TypeReference>(), delegate(HashSet<TypeReference> accum, ReadOnlyInflatedCollectionCollector item)
			{
				accum.UnionWith(item.InstantiatedGenericsAndArrays);
				return accum;
			}).ToSortedCollection();
			Methods = others.Aggregate(new HashSet<GenericInstanceMethod>(), delegate(HashSet<GenericInstanceMethod> accum, ReadOnlyInflatedCollectionCollector item)
			{
				accum.UnionWith(item.Methods);
				return accum;
			}).ToSortedCollection();
		}

		public static ReadOnlyInflatedCollectionCollector Merge(IEnumerable<ReadOnlyInflatedCollectionCollector> others)
		{
			return new ReadOnlyInflatedCollectionCollector(others);
		}
	}
}
