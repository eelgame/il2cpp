using Mono.Cecil;

namespace Unity.IL2CPP.Metadata.RuntimeTypes
{
	public class Il2CppGenericInstanceRuntimeType : Il2CppRuntimeTypeBase<GenericInstanceType>
	{
		public readonly IIl2CppRuntimeType[] GenericArguments;

		public readonly IIl2CppRuntimeType GenericTypeDefinition;

		public Il2CppGenericInstanceRuntimeType(GenericInstanceType type, int attrs, IIl2CppRuntimeType[] genericArguments, IIl2CppRuntimeType genericTypeDefinition)
			: base(type, attrs)
		{
			GenericArguments = genericArguments;
			GenericTypeDefinition = genericTypeDefinition;
		}
	}
}
