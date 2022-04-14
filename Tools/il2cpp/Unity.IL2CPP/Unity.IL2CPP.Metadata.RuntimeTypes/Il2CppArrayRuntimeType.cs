using Mono.Cecil;

namespace Unity.IL2CPP.Metadata.RuntimeTypes
{
	public class Il2CppArrayRuntimeType : Il2CppRuntimeTypeBase<ArrayType>
	{
		public readonly IIl2CppRuntimeType ElementType;

		public Il2CppArrayRuntimeType(ArrayType type, int attrs, IIl2CppRuntimeType elementType)
			: base(type, attrs)
		{
			ElementType = elementType;
		}
	}
}
