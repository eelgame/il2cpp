using Mono.Cecil;

namespace Unity.IL2CPP
{
	public struct InvokerSignature
	{
		public readonly bool HasThis;

		public readonly TypeReference[] ReducedParameterTypes;

		public InvokerSignature(bool hasThis, TypeReference[] reducedParameterTypes)
		{
			HasThis = hasThis;
			ReducedParameterTypes = reducedParameterTypes;
		}
	}
}
