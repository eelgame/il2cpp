using System.Text;
using Mono.Cecil;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.Marshaling.BodyWriters.ManagedToNative
{
	internal abstract class ComMethodBodyWriter : ManagedToNativeInteropMethodBodyWriter
	{
		protected readonly MethodReference _actualMethod;

		protected readonly MarshalType _marshalType;

		protected readonly TypeReference _interfaceType;

		public ComMethodBodyWriter(MinimalContext context, MethodReference actualMethod, MethodReference interfaceMethod)
			: base(context, interfaceMethod, actualMethod, GetMarshalType(interfaceMethod), useUnicodeCharset: true)
		{
			_actualMethod = actualMethod;
			_marshalType = GetMarshalType(interfaceMethod);
			_interfaceType = interfaceMethod.DeclaringType;
		}

		private static MarshalType GetMarshalType(MethodReference interfaceMethod)
		{
			if (!interfaceMethod.DeclaringType.IsComInterface())
			{
				return MarshalType.WindowsRuntime;
			}
			return MarshalType.COM;
		}

		protected override void WriteMethodPrologue(IGeneratedMethodCodeWriter writer, IRuntimeMetadataAccess metadataAccess)
		{
			string text = _context.Global.Services.Naming.ForTypeNameOnly(_interfaceType);
			string text2 = _context.Global.Services.Naming.ForInteropInterfaceVariable(_interfaceType);
			if (_actualMethod.HasThis)
			{
				string text3 = "static_cast<Il2CppComObject*>(__this)";
				writer.WriteLine(text + "* " + text2 + " = il2cpp_codegen_com_query_interface<" + text + ">(" + text3 + ");");
			}
			else
			{
				string text4 = _context.Global.Services.Naming.ForStaticFieldsStruct(_actualMethod.DeclaringType);
				string text5 = "((" + text4 + "*)il2cpp_codegen_static_fields_for(" + metadataAccess.TypeInfoFor(_actualMethod.DeclaringType) + "))";
				writer.WriteLine(text + "* " + text2 + " = " + text5 + "->" + _context.Global.Services.Naming.ForComTypeInterfaceFieldGetter(_interfaceType) + "();");
			}
			writer.AddIncludeForTypeDefinition(_interfaceType);
			writer.WriteLine();
		}

		protected override void WriteInteropCallStatement(IGeneratedMethodCodeWriter writer, string[] localVariableNames, IRuntimeMetadataAccess metadataAccess)
		{
			MethodReturnType methodReturnType = GetMethodReturnType();
			if (methodReturnType.ReturnType.MetadataType != MetadataType.Void)
			{
				MarshalInfoWriterFor(methodReturnType).WriteNativeVariableDeclarationOfType(writer, _context.Global.Services.Naming.ForInteropReturnValue());
			}
			writer.WriteStatement(GetMethodCallExpression(localVariableNames));
			if (!InteropMethod.Resolve().IsPreserveSig)
			{
				writer.WriteLine();
				writer.WriteStatement(Emit.Call("il2cpp_codegen_com_raise_exception_if_failed", _context.Global.Services.Naming.ForInteropHResultVariable(), (_marshalType == MarshalType.COM) ? "true" : "false"));
			}
		}

		private string GetMethodCallExpression(string[] localVariableNames)
		{
			bool isPreserveSig = InteropMethod.Resolve().IsPreserveSig;
			MethodReturnType methodReturnType = GetMethodReturnType();
			string functionCallParametersExpression = GetFunctionCallParametersExpression(localVariableNames, !isPreserveSig);
			StringBuilder stringBuilder = new StringBuilder();
			if (!isPreserveSig)
			{
				stringBuilder.Append("const il2cpp_hresult_t ");
				stringBuilder.Append(_context.Global.Services.Naming.ForInteropHResultVariable());
				stringBuilder.Append(" = ");
			}
			else if (methodReturnType.ReturnType.MetadataType != MetadataType.Void)
			{
				stringBuilder.Append(_context.Global.Services.Naming.ForInteropReturnValue());
				stringBuilder.Append(" = ");
			}
			stringBuilder.Append(_context.Global.Services.Naming.ForInteropInterfaceVariable(_interfaceType));
			stringBuilder.Append("->");
			stringBuilder.Append(GetMethodNameInGeneratedCode());
			stringBuilder.Append("(");
			stringBuilder.Append(functionCallParametersExpression);
			stringBuilder.Append(")");
			return stringBuilder.ToString();
		}
	}
}
