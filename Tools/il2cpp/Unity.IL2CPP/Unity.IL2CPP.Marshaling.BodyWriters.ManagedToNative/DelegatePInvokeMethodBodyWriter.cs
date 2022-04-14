using Mono.Cecil;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.Marshaling.BodyWriters.ManagedToNative
{
	internal class DelegatePInvokeMethodBodyWriter : PInvokeMethodBodyWriter
	{
		public DelegatePInvokeMethodBodyWriter(ReadOnlyContext context, MethodReference interopMethod)
			: base(context, interopMethod)
		{
		}

		protected override void WriteMethodPrologue(IGeneratedMethodCodeWriter writer, IRuntimeMetadataAccess metadataAccess)
		{
			string decoratedName = MarshaledReturnType.DecoratedName;
			writer.WriteLine("typedef {0} ({1} *{2})({3});", decoratedName, InteropMethodBodyWriter.GetDelegateCallingConvention(_methodDefinition.DeclaringType), _context.Global.Services.Naming.ForPInvokeFunctionPointerTypedef(), FormatParametersForTypedef());
			if (!_context.Global.Parameters.UsingTinyClassLibraries)
			{
				writer.WriteLine(_context.Global.Services.Naming.ForPInvokeFunctionPointerTypedef() + " " + _context.Global.Services.Naming.ForPInvokeFunctionPointerVariable() + " = reinterpret_cast<" + _context.Global.Services.Naming.ForPInvokeFunctionPointerTypedef() + ">(il2cpp_codegen_get_method_pointer(((RuntimeDelegate*)__this)->method));");
			}
			else
			{
				writer.WriteLine(_context.Global.Services.Naming.ForPInvokeFunctionPointerTypedef() + " " + _context.Global.Services.Naming.ForPInvokeFunctionPointerVariable() + " = reinterpret_cast<" + _context.Global.Services.Naming.ForPInvokeFunctionPointerTypedef() + ">(__this);");
			}
			writer.WriteLine();
		}

		public bool IsDelegatePInvokeWrapperNecessary()
		{
			if (_methodDefinition.Name != "Invoke")
			{
				return false;
			}
			TypeDefinition declaringType = _methodDefinition.DeclaringType;
			if (!declaringType.IsDelegate())
			{
				return false;
			}
			if (declaringType.HasGenericParameters)
			{
				return false;
			}
			if (_methodDefinition.HasGenericParameters)
			{
				return false;
			}
			if (_methodDefinition.ReturnType.IsGenericParameter)
			{
				return false;
			}
			if (!_methodDefinition.IsRuntime)
			{
				return false;
			}
			if (_methodDefinition.ReturnType.IsByReference)
			{
				return false;
			}
			return FirstOrDefaultUnmarshalableMarshalInfoWriter() == null;
		}
	}
}
