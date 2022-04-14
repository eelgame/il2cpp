using System;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using Unity.Cecil.Awesome;
using Unity.IL2CPP.Contexts;

namespace Unity.IL2CPP
{
	public static class TypeCollapser
	{
		public static TypeReference[] CollapseSignature(ReadOnlyContext context, MethodReference method)
		{
			if (method.ContainsGenericParameters())
			{
				throw new InvalidOperationException("Cannot collapse uninflated method " + method.FullName);
			}
			TypeResolver typeResolver = new TypeResolver(method.DeclaringType as GenericInstanceType, method as GenericInstanceMethod);
			TypeReference[] array = new TypeReference[method.Parameters.Count + 1];
			array[0] = CollapseType(context, typeResolver.ResolveReturnType(method).WithoutModifiers());
			for (int i = 0; i < method.Parameters.Count; i++)
			{
				array[i + 1] = CollapseType(context, typeResolver.ResolveParameterType(method, method.Parameters[i]).WithoutModifiers());
			}
			return array;
		}

		public static TypeReference CollapseType(ReadOnlyContext context, TypeReference type)
		{
			if (type.IsByReference || type.IsPointer)
			{
				return context.Global.Services.TypeProvider.SystemVoid.MakePointerType();
			}
			if (!type.IsValueType())
			{
				return type.Module.TypeSystem.Object;
			}
			if (type.IsEnum())
			{
				type = type.GetUnderlyingEnumType();
			}
			if (type.MetadataType == MetadataType.Byte)
			{
				return type.Module.TypeSystem.SByte;
			}
			if (type.MetadataType == MetadataType.UInt16)
			{
				return type.Module.TypeSystem.Int16;
			}
			if (type.MetadataType == MetadataType.UInt32)
			{
				return type.Module.TypeSystem.Int32;
			}
			if (type.MetadataType == MetadataType.UInt64)
			{
				return type.Module.TypeSystem.Int64;
			}
			if (type.MetadataType == MetadataType.Boolean)
			{
				return type.Module.TypeSystem.SByte;
			}
			if (type.MetadataType == MetadataType.Char)
			{
				return type.Module.TypeSystem.Int16;
			}
			if (type.MetadataType == MetadataType.UIntPtr)
			{
				return type.Module.TypeSystem.IntPtr;
			}
			return type;
		}
	}
}
