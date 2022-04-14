using Mono.Cecil;
using Unity.IL2CPP.Metadata.RuntimeTypes;

namespace Unity.IL2CPP.Metadata
{
	public class RGCTXEntry
	{
		public readonly RGCTXType Type;

		public readonly MemberReference MemberReference;

		public readonly int GenericParameterIndex;

		public readonly IIl2CppRuntimeType RuntimeType;

		public RGCTXEntry(RGCTXType type, IIl2CppRuntimeType runtimeType, int genericParameterIndex = -1)
			: this(type, runtimeType.Type, runtimeType, genericParameterIndex)
		{
		}

		public RGCTXEntry(RGCTXType type, MethodReference methodReference, int genericParameterIndex = -1)
			: this(type, methodReference, null, genericParameterIndex)
		{
		}

		private RGCTXEntry(RGCTXType type, MemberReference memberReference, IIl2CppRuntimeType runtimeType, int genericParameterIndex)
		{
			Type = type;
			MemberReference = memberReference;
			RuntimeType = runtimeType;
			GenericParameterIndex = genericParameterIndex;
		}
	}
}
