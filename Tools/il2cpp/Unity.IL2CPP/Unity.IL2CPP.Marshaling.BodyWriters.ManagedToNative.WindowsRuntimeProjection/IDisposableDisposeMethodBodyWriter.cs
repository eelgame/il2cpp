using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Unity.IL2CPP.Marshaling.BodyWriters.ManagedToNative.WindowsRuntimeProjection
{
	internal class IDisposableDisposeMethodBodyWriter
	{
		private readonly MethodDefinition _closeMethod;

		public IDisposableDisposeMethodBodyWriter(MethodDefinition closeMethod)
		{
			_closeMethod = closeMethod;
		}

		public void WriteDispose(MethodDefinition method)
		{
			ILProcessor iLProcessor = method.Body.GetILProcessor();
			iLProcessor.Emit(OpCodes.Ldarg_0);
			iLProcessor.Emit(OpCodes.Callvirt, method.DeclaringType.Module.ImportReference(_closeMethod));
			iLProcessor.Emit(OpCodes.Ret);
		}
	}
}
