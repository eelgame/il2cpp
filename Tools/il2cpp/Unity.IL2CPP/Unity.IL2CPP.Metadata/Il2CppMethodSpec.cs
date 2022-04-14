using Mono.Cecil;
using Unity.IL2CPP.Metadata.RuntimeTypes;

namespace Unity.IL2CPP.Metadata
{
	public class Il2CppMethodSpec
	{
		public readonly MethodReference GenericMethod;

		public readonly IIl2CppRuntimeType[] MethodGenericInstanceData;

		public readonly IIl2CppRuntimeType[] TypeGenericInstanceData;

		public Il2CppMethodSpec(MethodReference genericMethod)
			: this(genericMethod, null, null)
		{
		}

		public Il2CppMethodSpec(MethodReference genericMethod, IIl2CppRuntimeType[] methodGenericInstanceData, IIl2CppRuntimeType[] typeGenericInstanceData)
		{
			GenericMethod = genericMethod;
			MethodGenericInstanceData = methodGenericInstanceData;
			TypeGenericInstanceData = typeGenericInstanceData;
		}
	}
}
