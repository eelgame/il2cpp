using System;
using System.Collections.Generic;
using Mono.Cecil;

namespace Unity.IL2CPP.GenericsCollection
{
	public static class Inflater
	{
		public static TypeReference InflateType(GenericContext context, TypeReference typeReference)
		{
			TypeReference typeReference2 = InflateTypeWithoutException(context, typeReference);
			if (typeReference2 == null)
			{
				throw new InvalidOperationException("Unable to resolve a reference to the type '" + typeReference.FullName + "' in the assembly '" + typeReference.Module.Assembly.FullName + "'. Does this type exist in a different assembly in the project?");
			}
			return typeReference2;
		}

		public static GenericInstanceType InflateType(GenericContext context, TypeDefinition typeDefinition)
		{
			return ConstructGenericType(context, typeDefinition, typeDefinition.GenericParameters);
		}

		public static GenericInstanceType InflateType(GenericContext context, GenericInstanceType genericInstanceType)
		{
			GenericInstanceType genericInstanceType2 = ConstructGenericType(context, genericInstanceType.Resolve(), genericInstanceType.GenericArguments);
			genericInstanceType2.MetadataToken = genericInstanceType.MetadataToken;
			return genericInstanceType2;
		}

		public static TypeReference InflateTypeWithoutException(GenericContext context, TypeReference typeReference)
		{
			if (typeReference is GenericParameter genericParameter)
			{
				if (genericParameter.Type != 0)
				{
					return context.Method.GenericArguments[genericParameter.Position];
				}
				return context.Type.GenericArguments[genericParameter.Position];
			}
			if (typeReference is GenericInstanceType genericInstanceType)
			{
				return InflateType(context, genericInstanceType);
			}
			if (typeReference is ArrayType arrayType)
			{
				return new ArrayType(InflateType(context, arrayType.ElementType), arrayType.Rank);
			}
			if (typeReference is ByReferenceType byReferenceType)
			{
				return new ByReferenceType(InflateType(context, byReferenceType.ElementType));
			}
			if (typeReference is PointerType pointerType)
			{
				return new PointerType(InflateType(context, pointerType.ElementType));
			}
			if (typeReference is RequiredModifierType requiredModifierType)
			{
				return InflateTypeWithoutException(context, requiredModifierType.ElementType);
			}
			if (typeReference is OptionalModifierType optionalModifierType)
			{
				return InflateTypeWithoutException(context, optionalModifierType.ElementType);
			}
			return typeReference.Resolve();
		}

		private static GenericInstanceType ConstructGenericType(GenericContext context, TypeDefinition typeDefinition, IEnumerable<TypeReference> genericArguments)
		{
			GenericInstanceType genericInstanceType = new GenericInstanceType(typeDefinition);
			foreach (TypeReference genericArgument in genericArguments)
			{
				genericInstanceType.GenericArguments.Add(InflateType(context, genericArgument));
			}
			return genericInstanceType;
		}

		public static GenericInstanceMethod InflateMethod(GenericContext context, MethodDefinition methodDefinition)
		{
			TypeReference typeReference = methodDefinition.DeclaringType;
			if (typeReference.Resolve().HasGenericParameters)
			{
				typeReference = InflateType(context, methodDefinition.DeclaringType);
			}
			return ConstructGenericMethod(context, typeReference, methodDefinition, methodDefinition.GenericParameters);
		}

		public static GenericInstanceMethod InflateMethod(GenericContext context, GenericInstanceMethod genericInstanceMethod)
		{
			TypeReference declaringType = ((genericInstanceMethod.DeclaringType is GenericInstanceType genericInstanceType) ? InflateType(context, genericInstanceType) : InflateType(context, genericInstanceMethod.DeclaringType));
			return ConstructGenericMethod(context, declaringType, genericInstanceMethod.Resolve(), genericInstanceMethod.GenericArguments);
		}

		private static GenericInstanceMethod ConstructGenericMethod(GenericContext context, TypeReference declaringType, MethodDefinition method, IEnumerable<TypeReference> genericArguments)
		{
			MethodReference methodReference = new MethodReference(method.Name, method.ReturnType, declaringType)
			{
				HasThis = method.HasThis
			};
			foreach (GenericParameter genericParameter in method.GenericParameters)
			{
				methodReference.GenericParameters.Add(new GenericParameter(genericParameter.Name, methodReference));
			}
			foreach (ParameterDefinition parameter in method.Parameters)
			{
				methodReference.Parameters.Add(new ParameterDefinition(parameter.Name, parameter.Attributes, parameter.ParameterType));
			}
			if (methodReference.Resolve() == null)
			{
				throw new Exception();
			}
			GenericInstanceMethod genericInstanceMethod = new GenericInstanceMethod(methodReference);
			foreach (TypeReference genericArgument in genericArguments)
			{
				genericInstanceMethod.GenericArguments.Add(InflateType(context, genericArgument));
			}
			return genericInstanceMethod;
		}
	}
}
