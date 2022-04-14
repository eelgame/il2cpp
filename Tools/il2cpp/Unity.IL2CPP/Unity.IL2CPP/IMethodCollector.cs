using Mono.Cecil;

namespace Unity.IL2CPP
{
	public interface IMethodCollector
	{
		void Add(MethodReference method);
	}
}
