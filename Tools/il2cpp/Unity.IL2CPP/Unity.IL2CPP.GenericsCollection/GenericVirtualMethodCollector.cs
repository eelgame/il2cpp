using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Unity.Cecil.Awesome.Comparers;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Collectors;
using Unity.IL2CPP.GenericSharing;
using Unity.IL2CPP.Metadata;

namespace Unity.IL2CPP.GenericsCollection
{
	public class GenericVirtualMethodCollector
	{
		private class NewGenericVirtualMethodFilter
		{
			public readonly List<GenericInstanceMethod> NewMethods = new List<GenericInstanceMethod>();

			public void OnMethodAdded(GenericInstanceMethod method)
			{
				if (method.Resolve().IsVirtual)
				{
					NewMethods.Add(method);
				}
			}
		}

		public void Collect(PrimaryCollectionContext context, InflatedCollectionCollector generics, IEnumerable<TypeDefinition> types, IVTableBuilder vTableBuilder)
		{
			HashSet<TypeDefinition> typeDefinitionsWithGenericVirtualMethods = new HashSet<TypeDefinition>();
			HashSet<TypeDefinition> genericTypesImplementingGenericVirtualMethods = new HashSet<TypeDefinition>();
			HashSet<TypeReference> typesImplementingGenericVirtualMethods = new HashSet<TypeReference>(new TypeReferenceEqualityComparer());
			CollectTypesWithGenericVirtualMethods(types, typeDefinitionsWithGenericVirtualMethods, genericTypesImplementingGenericVirtualMethods, typesImplementingGenericVirtualMethods);
			CollectGenericTypesImplementingGenericVirtualMethods(generics, genericTypesImplementingGenericVirtualMethods, typesImplementingGenericVirtualMethods);
			Dictionary<TypeReference, HashSet<TypeReference>> typeToImplementors = MapBaseTypesToAllDerivedTypes(context, typesImplementingGenericVirtualMethods, typeDefinitionsWithGenericVirtualMethods);
			GenericInstanceMethod[] initialGenericVirtualMethods = generics.Methods.Items.Where((GenericInstanceMethod m) => m.Resolve().IsVirtual).ToArray();
			FindOverridenMethodsInTypes(context, generics, vTableBuilder, initialGenericVirtualMethods, typeToImplementors);
		}

		private static void CollectTypesWithGenericVirtualMethods(IEnumerable<TypeDefinition> types, HashSet<TypeDefinition> typeDefinitionsWithGenericVirtualMethods, HashSet<TypeDefinition> genericTypesImplementingGenericVirtualMethods, HashSet<TypeReference> typesImplementingGenericVirtualMethods)
		{
			foreach (TypeDefinition type in types)
			{
				if (!type.HasMethods || !type.Methods.Any((MethodDefinition m) => m.IsVirtual && m.HasGenericParameters))
				{
					continue;
				}
				typeDefinitionsWithGenericVirtualMethods.Add(type);
				if (!type.IsInterface)
				{
					if (type.HasGenericParameters)
					{
						genericTypesImplementingGenericVirtualMethods.Add(type);
					}
					else
					{
						typesImplementingGenericVirtualMethods.Add(type);
					}
				}
			}
		}

		private static void CollectGenericTypesImplementingGenericVirtualMethods(InflatedCollectionCollector generics, HashSet<TypeDefinition> genericTypesImplementingGenericVirtualMethods, HashSet<TypeReference> typesImplementingGenericVirtualMethods)
		{
			foreach (GenericInstanceType item2 in generics.Types.Items)
			{
				TypeDefinition item = item2.Resolve();
				if (genericTypesImplementingGenericVirtualMethods.Contains(item))
				{
					typesImplementingGenericVirtualMethods.Add(item2);
				}
			}
		}

		private static Dictionary<TypeReference, HashSet<TypeReference>> MapBaseTypesToAllDerivedTypes(MinimalContext context, HashSet<TypeReference> typesImplementingGenericVirtualMethods, HashSet<TypeDefinition> typeDefinitionsWithGenericVirtualMethods)
		{
			Dictionary<TypeReference, HashSet<TypeReference>> dictionary = new Dictionary<TypeReference, HashSet<TypeReference>>(new TypeReferenceEqualityComparer());
			foreach (TypeReference typesImplementingGenericVirtualMethod in typesImplementingGenericVirtualMethods)
			{
				for (TypeReference typeReference = typesImplementingGenericVirtualMethod; typeReference != null; typeReference = typeReference.GetBaseType(context))
				{
					if (typeDefinitionsWithGenericVirtualMethods.Contains(typeReference.Resolve()))
					{
						TypeReference key = typeReference;
						if (typeReference is GenericInstanceType type)
						{
							key = GenericSharingAnalysis.GetSharedType(context, type);
						}
						if (!dictionary.TryGetValue(key, out var value))
						{
							value = new HashSet<TypeReference>(new TypeReferenceEqualityComparer());
							dictionary.Add(key, value);
						}
						value.Add(typesImplementingGenericVirtualMethod);
					}
					foreach (TypeReference @interface in typeReference.GetInterfaces(context))
					{
						if (typeDefinitionsWithGenericVirtualMethods.Contains(@interface.Resolve()))
						{
							if (!dictionary.TryGetValue(@interface, out var value2))
							{
								value2 = new HashSet<TypeReference>(new TypeReferenceEqualityComparer());
								dictionary.Add(@interface, value2);
							}
							value2.Add(typesImplementingGenericVirtualMethod);
						}
					}
				}
			}
			return dictionary;
		}

		private static void FindOverridenMethodsInTypes(PrimaryCollectionContext context, InflatedCollectionCollector generics, IVTableBuilder vTableBuilder, IEnumerable<GenericInstanceMethod> initialGenericVirtualMethods, Dictionary<TypeReference, HashSet<TypeReference>> typeToImplementors)
		{
			IEnumerable<GenericInstanceMethod> enumerable = initialGenericVirtualMethods;
			for (int i = 0; i < 2; i++)
			{
				NewGenericVirtualMethodFilter newGenericVirtualMethodFilter = new NewGenericVirtualMethodFilter();
				generics.Methods.OnItemAdded += newGenericVirtualMethodFilter.OnMethodAdded;
				foreach (GenericInstanceMethod item in enumerable)
				{
					if (!typeToImplementors.TryGetValue(item.DeclaringType, out var value))
					{
						continue;
					}
					foreach (TypeReference item2 in value)
					{
						GenericInstanceMethod genericInstanceMethod = FindMethodInTypeThatOverrides(context, item, item2, vTableBuilder);
						if (genericInstanceMethod != null && !generics.Methods.Items.Contains(genericInstanceMethod))
						{
							GenericContextAwareVisitor.ProcessGenericMethod(context, genericInstanceMethod, generics);
						}
					}
				}
				generics.Methods.OnItemAdded -= newGenericVirtualMethodFilter.OnMethodAdded;
				enumerable = newGenericVirtualMethodFilter.NewMethods;
			}
		}

		private static GenericInstanceMethod FindMethodInTypeThatOverrides(MinimalContext context, GenericInstanceMethod potentiallyOverridenGenericInstanceMethod, TypeReference typeThatMightHaveAnOverrideingMethod, IVTableBuilder vTableBuilder)
		{
			MethodDefinition methodDefinition = potentiallyOverridenGenericInstanceMethod.Resolve();
			VTable vTable = vTableBuilder.VTableFor(context, typeThatMightHaveAnOverrideingMethod);
			int num = vTableBuilder.IndexFor(context, methodDefinition);
			if (methodDefinition.DeclaringType.IsInterface)
			{
				num += vTable.InterfaceOffsets[potentiallyOverridenGenericInstanceMethod.DeclaringType];
			}
			MethodReference methodReference = vTable.Slots[num];
			if (methodReference == null)
			{
				return null;
			}
			if (!TypeReferenceEqualityComparer.AreEqual(methodReference.DeclaringType, typeThatMightHaveAnOverrideingMethod))
			{
				return null;
			}
			return Inflater.InflateMethod(new GenericContext(typeThatMightHaveAnOverrideingMethod as GenericInstanceType, potentiallyOverridenGenericInstanceMethod), methodReference.Resolve());
		}
	}
}
