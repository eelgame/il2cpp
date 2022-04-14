using Mono.Cecil;
using Unity.IL2CPP.Contexts;

namespace Unity.IL2CPP.Debugger
{
	public interface ICatchPointCollector
	{
		void AddCatchPoint(PrimaryCollectionContext context, MethodDefinition method, ExceptionSupport.Node catchNode);
	}
}
