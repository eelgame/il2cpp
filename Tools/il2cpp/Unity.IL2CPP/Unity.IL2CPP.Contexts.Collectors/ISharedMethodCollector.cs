using Mono.Cecil;

namespace Unity.IL2CPP.Contexts.Collectors
{
	public interface ISharedMethodCollector
	{
		void AddSharedMethod(MethodReference sharedMethod, MethodReference actualMethod);
	}
}
