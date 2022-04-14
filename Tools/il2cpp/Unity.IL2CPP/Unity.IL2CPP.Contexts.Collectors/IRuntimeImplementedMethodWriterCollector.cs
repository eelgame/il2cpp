using Mono.Cecil;

namespace Unity.IL2CPP.Contexts.Collectors
{
	public interface IRuntimeImplementedMethodWriterCollector
	{
		void RegisterMethod(MethodDefinition method, GetGenericSharingDataDelegate getGenericSharingData, WriteRuntimeImplementedMethodBodyDelegate writeMethodBody);
	}
}
