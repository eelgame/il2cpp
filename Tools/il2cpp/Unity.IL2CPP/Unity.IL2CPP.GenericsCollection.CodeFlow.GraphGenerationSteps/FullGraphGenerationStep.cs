using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Unity.IL2CPP.GenericsCollection.CodeFlow.GraphGenerationData;

namespace Unity.IL2CPP.GenericsCollection.CodeFlow.GraphGenerationSteps
{
	internal static class FullGraphGenerationStep
	{
		public static void Run(ref GraphGenerationContext context)
		{
			foreach (AssemblyDefinition assembly in context.Assemblies)
			{
				foreach (TypeDefinition allType in assembly.MainModule.GetAllTypes())
				{
					GenerateCodeFlowGraph(ref context, allType);
					foreach (MethodDefinition method in allType.Methods)
					{
						GenerateCodeFlowGraph(ref context, method);
					}
				}
			}
		}

		private static void GenerateCodeFlowGraph(ref GraphGenerationContext context, TypeDefinition type)
		{
			int count = context.TypeDependencies.Count;
			int count2 = context.MethodDependencies.Count;
			int referrerIndex = context.TypeNodes.Count | int.MinValue;
			GenerateFromImplicitDependencies(ref context, type, referrerIndex);
			GenerateFromBaseType(ref context, type, referrerIndex);
			GenerateFromMethods(ref context, type, referrerIndex);
			StagingNode<TypeDefinition> item = new StagingNode<TypeDefinition>(type, count2, context.MethodDependencies.Count, count, context.TypeDependencies.Count);
			context.TypeNodes.Add(item);
		}

		private static void GenerateFromImplicitDependencies(ref GraphGenerationContext context, IMemberDefinition definition, int referrerIndex)
		{
			if (!context.ImplicitDependencies.TryGetValue(definition, out var value))
			{
				return;
			}
			foreach (GenericInstanceType item in value)
			{
				TypeReferenceDefinitionPair dependency = new TypeReferenceDefinitionPair(item.Resolve(), item, TypeDependencyKind.ImplicitDependency);
				context.TypeDependencies.Add(new StagingDependency<TypeReferenceDefinitionPair>(dependency, referrerIndex));
			}
		}

		private static void GenerateFromBaseType(ref GraphGenerationContext context, TypeDefinition type, int referrerIndex)
		{
			if (type.BaseType is GenericInstanceType genericInstanceType)
			{
				TypeReferenceDefinitionPair dependency = new TypeReferenceDefinitionPair(genericInstanceType.Resolve(), genericInstanceType, TypeDependencyKind.BaseTypeOrInterface);
				context.TypeDependencies.Add(new StagingDependency<TypeReferenceDefinitionPair>(dependency, referrerIndex));
			}
		}

		private static void GenerateFromMethods(ref GraphGenerationContext context, TypeDefinition type, int referrerIndex)
		{
			foreach (MethodDefinition method in type.Methods)
			{
				if (!method.HasGenericParameters && method.HasThis)
				{
					AddDependency(ref context, method, referrerIndex);
				}
			}
		}

		private static void GenerateCodeFlowGraph(ref GraphGenerationContext context, MethodDefinition method)
		{
			int count = context.TypeDependencies.Count;
			int count2 = context.MethodDependencies.Count;
			int count3 = context.MethodNodes.Count;
			GenerateFromImplicitDependencies(ref context, method, count3);
			GenerateFromMethodSignature(ref context, method, count3);
			GenerateFromMethodBody(ref context, method, count3);
			StagingNode<MethodDefinition> item = new StagingNode<MethodDefinition>(method, count2, context.MethodDependencies.Count, count, context.TypeDependencies.Count);
			context.MethodNodes.Add(item);
		}

		private static void GenerateFromMethodSignature(ref GraphGenerationContext context, MethodDefinition method, int referrerIndex)
		{
			if (method.ReturnType is GenericInstanceType typeReference)
			{
				AddDependency(ref context, typeReference, referrerIndex, TypeDependencyKind.MethodParameterOrReturnType);
			}
			foreach (ParameterDefinition parameter in method.Parameters)
			{
				if (parameter.ParameterType is GenericInstanceType typeReference2)
				{
					AddDependency(ref context, typeReference2, referrerIndex, TypeDependencyKind.MethodParameterOrReturnType);
				}
			}
		}

		private static void GenerateFromMethodBody(ref GraphGenerationContext context, MethodDefinition method, int referrerIndex)
		{
			if (!method.HasBody)
			{
				return;
			}
			foreach (Instruction instruction in method.Body.Instructions)
			{
				switch (instruction.OpCode.Code)
				{
				case Code.Call:
				case Code.Callvirt:
				case Code.Ldftn:
				case Code.Ldvirtftn:
				{
					MethodReference methodReference = (MethodReference)instruction.Operand;
					if (methodReference.DeclaringType.IsGenericInstance || methodReference.IsGenericInstance)
					{
						AddDependency(ref context, methodReference, referrerIndex);
					}
					break;
				}
				case Code.Box:
					if (instruction.Operand is GenericInstanceType typeReference2)
					{
						AddDependency(ref context, typeReference2, referrerIndex, TypeDependencyKind.InstantiatedGenericInstance);
					}
					break;
				case Code.Newobj:
					if (((MethodReference)instruction.Operand).DeclaringType is GenericInstanceType genericInstanceType)
					{
						TypeDefinition typeDefinition = genericInstanceType.Resolve();
						if (!typeDefinition.IsValueType)
						{
							AddDependency(ref context, typeDefinition, genericInstanceType, referrerIndex, TypeDependencyKind.InstantiatedGenericInstance);
						}
					}
					break;
				case Code.Newarr:
					if (context.ArraysAreOfInterest)
					{
						ArrayType typeReference = new ArrayType((TypeReference)instruction.Operand);
						AddDependency(ref context, null, typeReference, referrerIndex, TypeDependencyKind.InstantiatedArray);
					}
					break;
				}
			}
		}

		private static void AddDependency(ref GraphGenerationContext context, TypeReference typeReference, int referrerIndex, TypeDependencyKind kind)
		{
			AddDependency(ref context, typeReference.Resolve(), typeReference, referrerIndex, kind);
		}

		private static void AddDependency(ref GraphGenerationContext context, TypeDefinition typeDefinition, TypeReference typeReference, int referrerIndex, TypeDependencyKind kind)
		{
			TypeReferenceDefinitionPair dependency = new TypeReferenceDefinitionPair(typeDefinition, typeReference, kind);
			context.TypeDependencies.Add(new StagingDependency<TypeReferenceDefinitionPair>(dependency, referrerIndex));
		}

		private static void AddDependency(ref GraphGenerationContext context, MethodDefinition method, int referrerIndex)
		{
			context.MethodDependencies.Add(new StagingDependency<MethodReferenceDefinitionPair>(new MethodReferenceDefinitionPair(method, method), referrerIndex));
		}

		private static void AddDependency(ref GraphGenerationContext context, MethodReference method, int referrerIndex)
		{
			context.MethodDependencies.Add(new StagingDependency<MethodReferenceDefinitionPair>(new MethodReferenceDefinitionPair(method.Resolve(), method), referrerIndex));
		}
	}
}
