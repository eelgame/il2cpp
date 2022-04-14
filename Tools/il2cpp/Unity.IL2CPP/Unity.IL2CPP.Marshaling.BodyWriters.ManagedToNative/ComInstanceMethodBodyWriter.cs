using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Unity.IL2CPP.Contexts;

namespace Unity.IL2CPP.Marshaling.BodyWriters.ManagedToNative
{
	internal class ComInstanceMethodBodyWriter : ComMethodBodyWriter
	{
		public ComInstanceMethodBodyWriter(MinimalContext context, MethodReference method)
			: base(context, method, GetInterfaceMethod(context, method))
		{
		}

		private static MethodReference GetInterfaceMethod(ReadOnlyContext context, MethodReference method)
		{
			TypeReference declaringType = method.DeclaringType;
			if (declaringType.IsInterface())
			{
				return method;
			}
			TypeReference[] staticInterfaces = declaringType.GetAllFactoryTypes(context).ToArray();
			IEnumerable<TypeReference> candidateInterfaces = from iface in declaringType.GetInterfaces(context)
				where !staticInterfaces.Any((TypeReference nonInstanceInterface) => iface == nonInstanceInterface)
				select iface;
			return method.GetOverriddenInterfaceMethod(candidateInterfaces) ?? throw new InvalidOperationException($"Could not find overridden method for {method.FullName}");
		}
	}
}
