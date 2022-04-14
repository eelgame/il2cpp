using Mono.Cecil;
using Unity.IL2CPP.Metadata.RuntimeTypes;

namespace Unity.IL2CPP.Metadata
{
	public class UncollectedIl2CppRuntimeType : Il2CppRuntimeTypeBase<TypeReference>
	{
		public UncollectedIl2CppRuntimeType(TypeReference type)
			: base(type, 0)
		{
		}
	}
}
