using Mono.Cecil;

namespace Unity.IL2CPP.Metadata.RuntimeTypes
{
	public class Il2CppByReferenceRuntimeType : Il2CppRuntimeTypeBase<ByReferenceType>
	{
		public readonly IIl2CppRuntimeType ElementType;

		public Il2CppByReferenceRuntimeType(ByReferenceType type, int attrs, IIl2CppRuntimeType elementType)
			: base(type, attrs)
		{
			ElementType = elementType;
		}
	}
}
