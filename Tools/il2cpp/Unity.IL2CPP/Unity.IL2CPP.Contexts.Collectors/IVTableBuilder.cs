using Mono.Cecil;
using Unity.IL2CPP.Metadata;

namespace Unity.IL2CPP.Contexts.Collectors
{
	public interface IVTableBuilder
	{
		int IndexFor(ReadOnlyContext context, MethodDefinition method);

		VTable VTableFor(ReadOnlyContext context, TypeReference typeReference);

		MethodReference GetVirtualMethodTargetMethodForConstrainedCallOnValueType(ReadOnlyContext context, TypeReference type, MethodReference method);
	}
}
