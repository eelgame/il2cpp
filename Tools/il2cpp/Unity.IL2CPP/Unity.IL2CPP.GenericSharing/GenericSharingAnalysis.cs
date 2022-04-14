using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Unity.Cecil.Awesome;
using Unity.Cecil.Awesome.Comparers;
using Unity.IL2CPP.Contexts;

namespace Unity.IL2CPP.GenericSharing
{
	public static class GenericSharingAnalysis
	{
		public static bool NeedsTypeContextAsArgument(MethodReference method)
		{
			if (!method.Resolve().IsStatic)
			{
				return method.DeclaringType.IsValueType();
			}
			return true;
		}

		public static bool IsSharedMethod(ReadOnlyContext context, MethodReference method)
		{
			if (CanShareMethod(context, method))
			{
				return MethodReferenceComparer.AreEqual(method, GetSharedMethod(context, method));
			}
			return false;
		}

		public static bool CanShareMethod(ReadOnlyContext context, MethodReference method)
		{
			if (context.Global.Parameters.DisableGenericSharing)
			{
				return false;
			}
			if (!method.IsGenericInstance && !method.DeclaringType.IsGenericInstance)
			{
				return false;
			}
			if (method.DeclaringType is GenericInstanceType type)
			{
				return CanShareType(context, type);
			}
			return true;
		}

		public static bool CanShareType(ReadOnlyContext context, GenericInstanceType type)
		{
			if (context.Global.Parameters.DisableGenericSharing)
			{
				return false;
			}
			if (GenericsUtilities.CheckForMaximumRecursion(context, type))
			{
				return false;
			}
			if (type.IsComOrWindowsRuntimeInterface(context))
			{
				return false;
			}
			return true;
		}

		public static bool IsGenericSharingForValueTypesEnabled(ReadOnlyContext context)
		{
			return context.Global.Parameters.CanShareEnumTypes;
		}

		public static GenericInstanceType GetSharedType(ReadOnlyContext context, TypeReference type)
		{
			if (context.Global.Parameters.DisableGenericSharing)
			{
				return (GenericInstanceType)type;
			}
			TypeDefinition typeDefinition = type.Resolve();
			TypeResolver typeResolver = TypeResolver.For(type);
			GenericInstanceType genericInstanceType = new GenericInstanceType(typeDefinition);
			foreach (GenericParameter genericParameter in typeDefinition.GenericParameters)
			{
				genericInstanceType.GenericArguments.Add(GetSharedTypeForGenericParameter(context, typeResolver, genericParameter));
			}
			return genericInstanceType;
		}

		private static TypeReference GetSharedTypeForGenericParameter(ReadOnlyContext context, TypeResolver typeResolver, GenericParameter genericParameter)
		{
			return GetUnderlyingSharedType(context, typeResolver.Resolve(genericParameter));
		}

		public static TypeReference GetUnderlyingSharedType(ReadOnlyContext context, TypeReference inflatedType)
		{
			if (context.Global.Parameters.DisableGenericSharing)
			{
				return inflatedType;
			}
			if (IsGenericSharingForValueTypesEnabled(context) && inflatedType.IsEnum())
			{
				inflatedType = context.Global.Services.TypeProvider.GetSharedEnumType(inflatedType);
			}
			if (inflatedType.IsValueType())
			{
				if (inflatedType.IsGenericInstance)
				{
					return GetSharedType(context, inflatedType);
				}
				return inflatedType;
			}
			return inflatedType.Module.TypeSystem.Object;
		}

		public static MethodReference GetSharedMethod(ReadOnlyContext context, MethodReference method)
		{
			if (context.Global.Parameters.DisableGenericSharing)
			{
				return method;
			}
			TypeReference typeReference = method.DeclaringType;
			if (typeReference.IsGenericInstance || typeReference.HasGenericParameters)
			{
				typeReference = GetSharedType(context, method.DeclaringType);
			}
			MethodReference methodReference = new MethodReference(method.Name, method.ReturnType, typeReference);
			foreach (GenericParameter genericParameter in method.Resolve().GenericParameters)
			{
				methodReference.GenericParameters.Add(new GenericParameter(genericParameter.Name, methodReference));
			}
			methodReference.CallingConvention = method.CallingConvention;
			methodReference.ExplicitThis = method.ExplicitThis;
			methodReference.HasThis = method.HasThis;
			foreach (ParameterDefinition parameter in method.Parameters)
			{
				methodReference.Parameters.Add(new ParameterDefinition(parameter.Name, parameter.Attributes, parameter.ParameterType));
			}
			if (method.IsGenericInstance || method.HasGenericParameters)
			{
				TypeResolver typeResolver = TypeResolver.For(method.DeclaringType, method);
				GenericInstanceMethod genericInstanceMethod = new GenericInstanceMethod(methodReference);
				foreach (GenericParameter genericParameter2 in method.Resolve().GenericParameters)
				{
					genericInstanceMethod.GenericArguments.Add(GetSharedTypeForGenericParameter(context, typeResolver, genericParameter2));
				}
				methodReference = genericInstanceMethod;
			}
			if (methodReference.Resolve() == null)
			{
				throw new Exception("Failed to resolve shared generic method");
			}
			return methodReference;
		}

		public static bool AreFullySharableGenericParameters(ReadOnlyContext context, IEnumerable<GenericParameter> genericParameters)
		{
			if (context.Global.Parameters.DisableGenericSharing)
			{
				return false;
			}
			return genericParameters.All((GenericParameter gp) => !gp.HasNotNullableValueTypeConstraint);
		}

		public static GenericInstanceType GetFullySharedType(TypeDefinition typeDefinition)
		{
			GenericInstanceType genericInstanceType = new GenericInstanceType(typeDefinition);
			for (int i = 0; i < typeDefinition.GenericParameters.Count; i++)
			{
				genericInstanceType.GenericArguments.Add(typeDefinition.Module.TypeSystem.Object);
			}
			return genericInstanceType;
		}

		public static MethodReference GetFullySharedMethod(MethodDefinition method)
		{
			if (!method.HasGenericParameters && !method.DeclaringType.HasGenericParameters)
			{
				throw new ArgumentException($"Attempting to get a fully shared method for method '{method.FullName}' which does not have any generic parameters");
			}
			TypeReference declaringType = (method.DeclaringType.HasGenericParameters ? ((TypeReference)GetFullySharedType(method.DeclaringType)) : ((TypeReference)method.DeclaringType));
			MethodReference methodReference = new MethodReference(method.Name, method.ReturnType, declaringType);
			foreach (GenericParameter genericParameter in method.Resolve().GenericParameters)
			{
				methodReference.GenericParameters.Add(new GenericParameter(genericParameter.Name, methodReference));
			}
			methodReference.CallingConvention = method.CallingConvention;
			methodReference.ExplicitThis = method.ExplicitThis;
			methodReference.HasThis = method.HasThis;
			foreach (ParameterDefinition parameter in method.Parameters)
			{
				methodReference.Parameters.Add(new ParameterDefinition(parameter.Name, parameter.Attributes, parameter.ParameterType));
			}
			if (method.IsGenericInstance || method.HasGenericParameters)
			{
				GenericInstanceMethod genericInstanceMethod = new GenericInstanceMethod(methodReference);
				for (int i = 0; i < method.GenericParameters.Count; i++)
				{
					genericInstanceMethod.GenericArguments.Add(method.DeclaringType.Module.TypeSystem.Object);
				}
				methodReference = genericInstanceMethod;
			}
			if (methodReference.Resolve() == null)
			{
				throw new Exception($"Failed to resolve shared generic instance method '{methodReference.FullName}' constructed from method definition '{method.FullName}'");
			}
			return methodReference;
		}

		public static bool ShouldTryToCallStaticConstructorBeforeMethodCall(ReadOnlyContext context, MethodReference targetMethod, MethodReference invokingMethod)
		{
			if (!targetMethod.HasThis)
			{
				return true;
			}
			if (!invokingMethod.Resolve().IsConstructor)
			{
				return false;
			}
			return TypeReferenceEqualityComparer.AreEqual(targetMethod.DeclaringType, invokingMethod.DeclaringType.GetBaseType(context));
		}

		public static TypeReference GetFullySharedTypeForGenericParameter(GenericParameter genericParameter)
		{
			if (genericParameter.HasNotNullableValueTypeConstraint)
			{
				throw new InvalidOperationException($"Attempting to share generic parameter '{genericParameter.FullName}' which has a value type constraint.");
			}
			return genericParameter.Module.TypeSystem.Object;
		}
	}
}
