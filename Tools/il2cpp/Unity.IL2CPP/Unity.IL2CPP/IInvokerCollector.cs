using Mono.Cecil;
using Unity.IL2CPP.Contexts;

namespace Unity.IL2CPP
{
	public interface IInvokerCollector
	{
		string Add(ReadOnlyContext context, MethodReference method);
	}
}
