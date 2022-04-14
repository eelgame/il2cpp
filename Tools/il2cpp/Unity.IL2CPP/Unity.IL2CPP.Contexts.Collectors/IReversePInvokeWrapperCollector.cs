using Mono.Cecil;

namespace Unity.IL2CPP.Contexts.Collectors
{
	public interface IReversePInvokeWrapperCollector
	{
		void AddReversePInvokeWrapper(MethodReference method);
	}
}
