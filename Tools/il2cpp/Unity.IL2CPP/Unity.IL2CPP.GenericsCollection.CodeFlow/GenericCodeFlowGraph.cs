using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using Unity.Cecil.Awesome;
using Unity.Cecil.Awesome.Comparers;
using Unity.IL2CPP.Contexts;
using Unity.MiniProfiling;

namespace Unity.IL2CPP.GenericsCollection.CodeFlow
{
	public struct GenericCodeFlowGraph
	{
		private struct CollectionContext
		{
			public readonly InflatedCollectionCollector Generics;

			public readonly HashSet<TypeReference> VisitedTypes;

			public readonly HashSet<MethodReference> VisitedMethods;

			public readonly List<TypeReference> TypesForCCWs;

			public readonly List<GenericInstanceType> FoundGenericTypes;

			public CollectionContext(InflatedCollectionCollector generics)
			{
				Generics = generics;
				VisitedMethods = new HashSet<MethodReference>(new MethodReferenceComparer());
				TypeReferenceEqualityComparer comparer = new TypeReferenceEqualityComparer();
				VisitedTypes = new HashSet<TypeReference>(comparer);
				TypesForCCWs = new List<TypeReference>();
				FoundGenericTypes = new List<GenericInstanceType>();
			}
		}

		private readonly IEnumerable<AssemblyDefinition> AllAssemblies;

		private readonly Node<MethodDefinition>[] MethodNodes;

		private readonly Node<TypeDefinition>[] TypeNodes;

		private readonly MethodDependency[] MethodDependencies;

		private readonly TypeDependency[] TypeDependencies;

		private readonly Dictionary<TypeDefinition, int> TypeIndices;

		internal GenericCodeFlowGraph(IEnumerable<AssemblyDefinition> allAssemblies, Node<MethodDefinition>[] methodNodes, Node<TypeDefinition>[] typeNodes, List<MethodDependency> methodDependencies, List<TypeDependency> typeDependencies, Dictionary<TypeDefinition, int> typeIndices)
		{
			AllAssemblies = allAssemblies;
			MethodNodes = methodNodes;
			TypeNodes = typeNodes;
			MethodDependencies = new MethodDependency[methodDependencies.Count];
			methodDependencies.CopyTo(MethodDependencies);
			TypeDependencies = new TypeDependency[typeDependencies.Count];
			typeDependencies.CopyTo(TypeDependencies);
			TypeIndices = typeIndices;
		}

		public void CollectGenerics(PrimaryCollectionContext context, InflatedCollectionCollector generics)
		{
			CollectionContext collectionContext = new CollectionContext(generics);
			using (MiniProfiler.Section("GenericCodeFlowGraph.CollectGenerics"))
			{
				CollectGenerics(context, ref collectionContext);
			}
			using (MiniProfiler.Section("GenericCodeFlowGraph.CollectCCWs"))
			{
				CollectCCWs(context, ref collectionContext);
			}
			using (MiniProfiler.Section("GenericCodeFlowGraph.DispatchToGenericContextAwareVisitor"))
			{
				DispatchToGenericContextAwareVisitor(context, ref collectionContext);
			}
		}

		private void CollectCCWs(ReadOnlyContext context, ref CollectionContext collectionContext)
		{
			foreach (AssemblyDefinition allAssembly in AllAssemblies)
			{
				foreach (TypeDefinition allType in allAssembly.MainModule.GetAllTypes())
				{
					if (!allType.HasGenericParameters)
					{
						CollectCCWsForType(context, ref collectionContext, allType);
					}
				}
			}
			for (int i = 0; i < collectionContext.TypesForCCWs.Count; i++)
			{
				CollectCCWsForType(context, ref collectionContext, collectionContext.TypesForCCWs[i]);
			}
		}

		private void CollectCCWsForType(ReadOnlyContext context, ref CollectionContext collectionContext, TypeReference type)
		{
			if (!type.NeedsComCallableWrapper(context))
			{
				return;
			}
			foreach (TypeReference item in type.GetInterfacesImplementedByComCallableWrapper(context))
			{
				if (item is GenericInstanceType && TypeIndices.TryGetValue(item.Resolve(), out var value) && value != -1)
				{
					CollectGenericsRecursive(context, ref collectionContext, item, TypeNodes[value]);
				}
			}
		}

		private void CollectGenerics(ReadOnlyContext context, ref CollectionContext collectionContext)
		{
			int num = TypeNodes.Length;
			for (int i = 0; i < num; i++)
			{
				Node<TypeDefinition> node = TypeNodes[i];
				TypeDefinition item = node.Item;
				if (!item.HasGenericParameters)
				{
					CollectGenericsRecursive(context, ref collectionContext, item, node);
				}
			}
			int num2 = MethodNodes.Length;
			for (int j = 0; j < num2; j++)
			{
				Node<MethodDefinition> node2 = MethodNodes[j];
				MethodDefinition item2 = node2.Item;
				if (!item2.HasGenericParameters && !item2.DeclaringType.HasGenericParameters)
				{
					CollectGenericsRecursive(context, ref collectionContext, item2, node2);
				}
			}
		}

		private void CollectGenericsRecursive(ReadOnlyContext context, ref CollectionContext collectionContext, TypeReference type, Node<TypeDefinition> node)
		{
			if ((!(type is GenericInstanceType genericInstance) || !GenericsUtilities.CheckForMaximumRecursion(context, genericInstance)) && collectionContext.VisitedTypes.Add(type))
			{
				TypeResolver resolver = TypeResolver.For(type);
				CollectTypeDependencies(context, ref collectionContext, resolver, node.TypeDependenciesStartIndex, node.TypeDependenciesEndIndex);
				CollectMethodDependencies(context, ref collectionContext, resolver, node.MethodDependenciesStartIndex, node.MethodDependenciesEndIndex);
			}
		}

		private void CollectGenericsRecursive(ReadOnlyContext context, ref CollectionContext collectionContext, MethodReference method, Node<MethodDefinition> node)
		{
			if ((!(method is GenericInstanceMethod genericInstance) || !GenericsUtilities.CheckForMaximumRecursion(context, genericInstance)) && (!(method.DeclaringType is GenericInstanceType genericInstance2) || !GenericsUtilities.CheckForMaximumRecursion(context, genericInstance2)) && collectionContext.VisitedMethods.Add(method))
			{
				TypeResolver resolver = TypeResolver.For(method.DeclaringType, method);
				CollectTypeDependencies(context, ref collectionContext, resolver, node.TypeDependenciesStartIndex, node.TypeDependenciesEndIndex);
				CollectMethodDependencies(context, ref collectionContext, resolver, node.MethodDependenciesStartIndex, node.MethodDependenciesEndIndex);
			}
		}

		private void CollectTypeDependencies(ReadOnlyContext context, ref CollectionContext collectionContext, TypeResolver resolver, int typeDependenciesStartIndex, int typeDependenciesEndIndex)
		{
			for (int i = typeDependenciesStartIndex; i < typeDependenciesEndIndex; i++)
			{
				TypeDependency typeDependency = TypeDependencies[i];
				TypeReference typeReference = resolver.Resolve(typeDependency.Type);
				if (HasFlag(typeDependency.Kind, TypeDependencyKind.InstantiatedArray))
				{
					if (collectionContext.Generics.InstantiatedGenericsAndArrays.Add((ArrayType)typeReference))
					{
						collectionContext.TypesForCCWs.Add(typeReference);
					}
				}
				else if (HasFlag(typeDependency.Kind, TypeDependencyKind.IsOfInterest))
				{
					GenericInstanceType genericInstanceType = (GenericInstanceType)typeReference;
					if ((HasFlag(typeDependency.Kind, TypeDependencyKind.InstantiatedGenericInstance | TypeDependencyKind.ImplicitDependency) || (HasFlag(typeDependency.Kind, TypeDependencyKind.MethodParameterOrReturnType) && genericInstanceType.IsValueType())) && collectionContext.Generics.InstantiatedGenericsAndArrays.Add(genericInstanceType))
					{
						collectionContext.TypesForCCWs.Add(typeReference);
					}
					collectionContext.FoundGenericTypes.Add(genericInstanceType);
				}
				if (typeDependency.DefinitionIndex != -1)
				{
					CollectGenericsRecursive(context, ref collectionContext, typeReference, TypeNodes[typeDependency.DefinitionIndex]);
				}
			}
		}

		private void CollectMethodDependencies(ReadOnlyContext context, ref CollectionContext collectionContext, TypeResolver resolver, int methodDependenciesStartIndex, int methodDependenciesEndIndex)
		{
			for (int i = methodDependenciesStartIndex; i < methodDependenciesEndIndex; i++)
			{
				MethodDependency methodDependency = MethodDependencies[i];
				MethodReference methodReference = resolver.Resolve(methodDependency.Method);
				if (methodDependency.IsOfInterest && methodReference.DeclaringType is GenericInstanceType item)
				{
					collectionContext.FoundGenericTypes.Add(item);
				}
				if (methodDependency.DefinitionIndex != -1)
				{
					CollectGenericsRecursive(context, ref collectionContext, methodReference, MethodNodes[methodDependency.DefinitionIndex]);
				}
			}
		}

		private void DispatchToGenericContextAwareVisitor(PrimaryCollectionContext context, ref CollectionContext collectionContext)
		{
			InflatedCollectionCollector generics = collectionContext.Generics;
			List<GenericInstanceType> foundGenericTypes = collectionContext.FoundGenericTypes;
			int count = foundGenericTypes.Count;
			for (int i = 0; i < count; i++)
			{
				GenericInstanceType genericInstanceType = foundGenericTypes[i];
				if (generics.TypeDeclarations.Add(genericInstanceType))
				{
					GenericContextAwareVisitor.ProcessGenericType(context, genericInstanceType, generics, null);
				}
			}
		}

		private static bool HasFlag(TypeDependencyKind value, TypeDependencyKind flag)
		{
			return (value & flag) != 0;
		}
	}
}
