using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using Mono.Collections.Generic;
using Unity.Cecil.Awesome;
using Unity.Cecil.Awesome.Comparers;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.GenericsCollection.CodeFlow.GraphGenerationData;
using Unity.MiniProfiling;

namespace Unity.IL2CPP.GenericsCollection.CodeFlow
{
	internal static class GenericCodeFlowGraphCollector
	{
		public static InflatedCollectionCollector Collect(PrimaryCollectionContext context, IEnumerable<AssemblyDefinition> assemblies)
		{
			using (MiniProfiler.Section("GenericCodeFlowGraphCollector.Collect"))
			{
				InputData allInputDatas;
				using (MiniProfiler.Section("GenericCodeFlowGraphCollector.GetTypesAndMethodsForAnalysis"))
				{
					allInputDatas = CollectGenericCodeFlowGlobalInputData(context);
					MergeInputData(ref allInputDatas, assemblies.Select((AssemblyDefinition assembly) => GetTypesAndMethodsForAnalysis(context, assembly)));
				}
				InflatedCollectionCollector inflatedCollectionCollector = new InflatedCollectionCollector();
				if (allInputDatas.DefinitionsOfInterest.Count > 0)
				{
					GenericCodeFlowGraphGenerator.Generate(ref allInputDatas, assemblies).CollectGenerics(context, inflatedCollectionCollector);
				}
				return inflatedCollectionCollector;
			}
		}

		private static void MergeInputData(ref InputData allInputDatas, IEnumerable<InputData> inputDatas)
		{
			foreach (InputData inputData in inputDatas)
			{
				foreach (IMemberDefinition item in inputData.DefinitionsOfInterest)
				{
					allInputDatas.DefinitionsOfInterest.Add(item);
				}
				if (inputData.ImplicitDependencies != null)
				{
					MergeDictionaries(allInputDatas.ImplicitDependencies, inputData.ImplicitDependencies);
				}
			}
		}

		private static void MergeDictionaries<TKey, TValue>(Dictionary<TKey, List<TValue>> outDictionary, Dictionary<TKey, List<TValue>> inDictionary)
		{
			foreach (KeyValuePair<TKey, List<TValue>> item in inDictionary)
			{
				if (outDictionary.TryGetValue(item.Key, out var value))
				{
					value.AddRange(item.Value);
				}
				else
				{
					outDictionary.Add(item.Key, item.Value);
				}
			}
		}

		private static InputData GetTypesAndMethodsForAnalysis(ReadOnlyContext context, AssemblyDefinition assembly)
		{
			HashSet<IMemberDefinition> hashSet = new HashSet<IMemberDefinition>();
			Dictionary<IMemberDefinition, List<GenericInstanceType>> dictionary = new Dictionary<IMemberDefinition, List<GenericInstanceType>>();
			HashSet<GenericInstanceType> hashSet2 = new HashSet<GenericInstanceType>(new TypeReferenceEqualityComparer());
			foreach (TypeDefinition allType in assembly.MainModule.GetAllTypes())
			{
				if (!allType.HasGenericParameters)
				{
					if (allType.IsWindowsRuntime)
					{
						hashSet2.Clear();
						CollectAllImplementedGenericInterfaces(allType, hashSet2);
						if (hashSet2.Count > 0)
						{
							dictionary[allType] = new List<GenericInstanceType>(hashSet2);
						}
					}
				}
				else
				{
					if (allType.IsInterface || allType.IsAbstract)
					{
						continue;
					}
					if (allType.IsDelegate() && allType.IsWindowsRuntime)
					{
						hashSet.Add(allType);
						continue;
					}
					GenericInstanceType genericInstanceType = new GenericInstanceType(allType);
					for (int i = 0; i < allType.GenericParameters.Count; i++)
					{
						genericInstanceType.GenericArguments.Add(allType.Module.TypeSystem.Object);
					}
					if (genericInstanceType.NeedsComCallableWrapper(context))
					{
						hashSet.Add(allType);
					}
				}
			}
			return new InputData(hashSet, dictionary, arraysAreOfInterest: false);
		}

		private static void CollectAllImplementedGenericInterfaces(TypeReference type, HashSet<GenericInstanceType> results)
		{
			TypeResolver typeResolver = TypeResolver.For(type);
			foreach (InterfaceImplementation @interface in type.Resolve().Interfaces)
			{
				if (@interface.InterfaceType is GenericInstanceType typeReference)
				{
					GenericInstanceType genericInstanceType = (GenericInstanceType)typeResolver.Resolve(typeReference);
					results.Add(genericInstanceType);
					CollectAllImplementedGenericInterfaces(genericInstanceType, results);
				}
			}
		}

		private static InputData CollectGenericCodeFlowGlobalInputData(PrimaryCollectionContext context)
		{
			HashSet<IMemberDefinition> hashSet = new HashSet<IMemberDefinition>();
			Dictionary<IMemberDefinition, List<GenericInstanceType>> dictionary = new Dictionary<IMemberDefinition, List<GenericInstanceType>>();
			bool arraysAreOfInterest = false;
			ITypeProviderService typeProvider = context.Global.Services.TypeProvider;
			foreach (KeyValuePair<TypeDefinition, TypeDefinition> clrToWindowsRuntimeProjectedType in context.Global.Services.WindowsRuntime.GetClrToWindowsRuntimeProjectedTypes())
			{
				TypeDefinition key = clrToWindowsRuntimeProjectedType.Key;
				TypeDefinition value = clrToWindowsRuntimeProjectedType.Value;
				if (!key.HasGenericParameters || (!value.IsInterface && !value.IsDelegate()))
				{
					continue;
				}
				List<GenericInstanceType> list = new List<GenericInstanceType>();
				List<GenericInstanceType> list2 = new List<GenericInstanceType>();
				if (value.Namespace == "Windows.Foundation.Collections")
				{
					if (value.Name == "IMapView`2" && typeProvider.ConstantSplittableMapType != null)
					{
						list2.Add(MakeGenericInstanceTypeWithGenericParameters(value, typeProvider.ConstantSplittableMapType));
						hashSet.Add(typeProvider.ConstantSplittableMapType);
					}
					if (value.Name == "IMap`2")
					{
						TypeDefinition typeDefinition = typeProvider.OptionalResolve("System.Collections.ObjectModel", "ReadOnlyDictionary`2", typeProvider.Corlib.Name);
						if (typeDefinition == null)
						{
							throw new InvalidProgramException("Windows.Foundation.Collections.IMap`2 was not stripped but System.Collections.ObjectModel.ReadOnlyDictionary`2 was. This indicates a bug in UnityLinker.");
						}
						list2.Add(MakeGenericInstanceTypeWithGenericParameters(value, typeDefinition));
						hashSet.Add(typeDefinition);
					}
					if (value.Name == "IVector`1")
					{
						TypeDefinition typeDefinition2 = typeProvider.OptionalResolve("System.Collections.ObjectModel", "ReadOnlyCollection`1", typeProvider.Corlib.Name);
						if (typeDefinition2 == null)
						{
							throw new InvalidProgramException("Windows.Foundation.Collections.IVector`1 was not stripped but System.Collections.ObjectModel.ReadOnlyCollection`1 was. This indicates a bug in UnityLinker.");
						}
						list2.Add(MakeGenericInstanceTypeWithGenericParameters(value, typeDefinition2));
						hashSet.Add(typeDefinition2);
						arraysAreOfInterest = true;
					}
					if (value.Name == "IVectorView`1")
					{
						arraysAreOfInterest = true;
					}
					if (value.Name == "IIterable`1")
					{
						TypeDefinition typeDefinition3 = typeProvider.OptionalResolve("Windows.Foundation.Collections", "IIterator`1", value.Module.Assembly.Name);
						if (typeDefinition3 == null)
						{
							throw new InvalidProgramException("Windows.Foundation.Collections.IIterable`1 was not stripped but Windows.Foundation.Collections.IIterator`1 was. This indicates a bug in UnityLinker.");
						}
						list2.Add(MakeGenericInstanceTypeWithGenericParameters(value, typeDefinition3));
						arraysAreOfInterest = true;
					}
				}
				list.Add(MakeGenericInstanceTypeWithGenericParameters(key, value));
				list2.Add(MakeGenericInstanceTypeWithGenericParameters(value, key));
				hashSet.Add(value);
				dictionary.Add(key, list);
				dictionary.Add(value, list2);
				if (!key.IsInterface)
				{
					continue;
				}
				foreach (MethodDefinition method in key.Methods)
				{
					hashSet.Add(method);
				}
				foreach (MethodDefinition method2 in value.Methods)
				{
					hashSet.Add(method2);
				}
			}
			foreach (KeyValuePair<TypeDefinition, TypeDefinition> nativeToManagedInterfaceAdapterClass in context.Global.Services.WindowsRuntime.GetNativeToManagedInterfaceAdapterClasses())
			{
				if (!nativeToManagedInterfaceAdapterClass.Key.HasGenericParameters)
				{
					continue;
				}
				foreach (MethodDefinition method3 in nativeToManagedInterfaceAdapterClass.Key.Methods)
				{
					if (!dictionary.TryGetValue(method3, out var value2))
					{
						dictionary.Add(method3, value2 = new List<GenericInstanceType>());
					}
					value2.Add(MakeGenericInstanceTypeWithGenericParameters(nativeToManagedInterfaceAdapterClass.Key, nativeToManagedInterfaceAdapterClass.Value));
					hashSet.Add(nativeToManagedInterfaceAdapterClass.Value);
				}
			}
			return new InputData(hashSet, dictionary, arraysAreOfInterest);
		}

		private static GenericInstanceType MakeGenericInstanceTypeWithGenericParameters(IGenericParameterProvider genericParameterProvider, TypeDefinition type)
		{
			GenericInstanceType genericInstanceType = new GenericInstanceType(type);
			Collection<GenericParameter> genericParameters = genericParameterProvider.GenericParameters;
			int count = genericParameters.Count;
			for (int i = 0; i < count; i++)
			{
				genericInstanceType.GenericArguments.Add(genericParameters[i]);
			}
			return genericInstanceType;
		}
	}
}
