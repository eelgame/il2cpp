using Mono.Cecil;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.WindowsRuntime;

namespace Unity.IL2CPP.Marshaling.BodyWriters.ManagedToNative
{
	internal class WindowsRuntimeDelegateMethodBodyWriter : ComMethodBodyWriter
	{
		public WindowsRuntimeDelegateMethodBodyWriter(MinimalContext context, MethodReference invokeMethod)
			: base(context, invokeMethod, invokeMethod)
		{
		}

		protected override void WriteMethodPrologue(IGeneratedMethodCodeWriter writer, IRuntimeMetadataAccess metadataAccess)
		{
			string text = _context.Global.Services.Naming.ForWindowsRuntimeDelegateComCallableWrapperInterface(_interfaceType);
			string text2 = _context.Global.Services.Naming.ForInteropInterfaceVariable(_interfaceType);
			writer.WriteLine(text + "* " + text2 + " = il2cpp_codegen_com_query_interface<" + text + ">(static_cast<Il2CppComObject*>(__this));");
			writer.WriteLine();
		}

		protected override string GetMethodNameInGeneratedCode()
		{
			return "Invoke";
		}
	}
}
