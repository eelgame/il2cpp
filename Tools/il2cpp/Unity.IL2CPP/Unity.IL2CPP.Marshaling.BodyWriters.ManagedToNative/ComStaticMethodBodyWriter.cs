using System;
using Mono.Cecil;
using Unity.IL2CPP.Contexts;

namespace Unity.IL2CPP.Marshaling.BodyWriters.ManagedToNative
{
	internal class ComStaticMethodBodyWriter : ComMethodBodyWriter
	{
		public ComStaticMethodBodyWriter(MinimalContext context, MethodReference actualMethod)
			: base(context, actualMethod, GetInterfaceMethod(actualMethod))
		{
		}

		private static MethodReference GetInterfaceMethod(MethodReference method)
		{
			TypeDefinition typeDefinition = method.DeclaringType.Resolve();
			if (!typeDefinition.IsWindowsRuntime)
			{
				throw new InvalidOperationException("Calling static methods is not supported on COM classes!");
			}
			if (typeDefinition.HasGenericParameters)
			{
				throw new InvalidOperationException("Calling static methods is not supported on types with generic parameters!");
			}
			if (typeDefinition.IsInterface)
			{
				throw new InvalidOperationException("Calling static methods is not supported on interfaces!");
			}
			return method.GetOverriddenInterfaceMethod(typeDefinition.GetStaticFactoryTypes()) ?? throw new InvalidOperationException($"Could not find overridden method for {method.FullName}");
		}
	}
}
