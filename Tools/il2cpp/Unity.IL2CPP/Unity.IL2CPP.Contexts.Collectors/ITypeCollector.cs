using Mono.Cecil;
using Unity.IL2CPP.Metadata.RuntimeTypes;

namespace Unity.IL2CPP.Contexts.Collectors
{
	public interface ITypeCollector
	{
		IIl2CppRuntimeType Add(TypeReference type, int attrs = 0);
	}
}
