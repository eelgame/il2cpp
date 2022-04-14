using Mono.Cecil;

namespace Unity.IL2CPP.Metadata.RuntimeTypes
{
	public class Il2CppRuntimeType : Il2CppRuntimeTypeBase<TypeReference>
	{
		public Il2CppRuntimeType(TypeReference type, int attrs)
			: base(type, attrs)
		{
		}
	}
}
