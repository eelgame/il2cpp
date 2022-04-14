using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.IL2CPP.Metadata.RuntimeTypes;

namespace Unity.IL2CPP.Debugger
{
	public class CatchPointInfo
	{
		public readonly IIl2CppRuntimeType RuntimeType;

		public readonly MethodDefinition Method;

		public readonly int IlOffset;

		public readonly ExceptionHandler CatchHandler;

		public readonly int TryId;

		public readonly int ParentTryId;

		public CatchPointInfo(IIl2CppRuntimeType runtimeType, MethodDefinition method, ExceptionSupport.Node catchNode)
		{
			RuntimeType = runtimeType;
			Method = method;
			CatchHandler = catchNode.Handler;
			IlOffset = catchNode.Start.Offset;
			TryId = catchNode.Id;
			ParentTryId = catchNode.ParentTryNode?.Id ?? (-1);
		}
	}
}
