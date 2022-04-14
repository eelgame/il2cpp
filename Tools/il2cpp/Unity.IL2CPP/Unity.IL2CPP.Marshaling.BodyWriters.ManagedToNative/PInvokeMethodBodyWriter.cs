using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Marshaling.MarshalInfoWriters;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.Marshaling.BodyWriters.ManagedToNative
{
	internal class PInvokeMethodBodyWriter : ManagedToNativeInteropMethodBodyWriter
	{
		protected readonly MethodDefinition _methodDefinition;

		protected readonly PInvokeInfo _pinvokeInfo;

		internal const string FORCE_PINVOKE_INTERNAL = "FORCE_PINVOKE_INTERNAL";

		private static readonly ReadOnlyDictionary<string, string[]> _internalizedMethods = new Dictionary<string, string[]> { 
		{
			"MonoPosixHelper",
			new string[5] { "CreateZStream", "CloseZStream", "Flush", "ReadZStream", "WriteZStream" }
		} }.AsReadOnly();

		public PInvokeMethodBodyWriter(ReadOnlyContext context, MethodReference interopMethod)
			: base(context, interopMethod, interopMethod, MarshalType.PInvoke, MarshalingUtils.UseUnicodeAsDefaultMarshalingForStringParameters(interopMethod))
		{
			_methodDefinition = interopMethod.Resolve();
			_pinvokeInfo = _methodDefinition.PInvokeInfo;
		}

		internal static string FORCE_PINVOKE_lib_INTERNAL(string lib)
		{
			string text = Path.GetFileNameWithoutExtension(lib).Replace('-', '_').Replace('.', '_');
			return "FORCE_PINVOKE_" + text + "_INTERNAL";
		}

		public void WriteExternMethodDeclarationForInternalPInvoke(IGeneratedMethodCodeWriter writer)
		{
			if (CanInternalizeMethod())
			{
				bool forForcedInternalPInvoke = !ShouldInternalizeMethod();
				writer.AddInternalPInvokeMethodDeclaration(_pinvokeInfo.EntryPoint, $"IL2CPP_EXTERN_C {FormatReturnTypeForTypedef()} {GetCallingConvention()} {_pinvokeInfo.EntryPoint}({FormatParametersForTypedef()});", _pinvokeInfo.Module.Name, forForcedInternalPInvoke, IsMethodExplicitlyMarkedInternal());
			}
		}

		protected override void WriteMethodPrologue(IGeneratedMethodCodeWriter writer, IRuntimeMetadataAccess metadataAccess)
		{
			writer.WriteLine("typedef {0};", GetPInvokeMethodVariable());
			if (ShouldInternalizeMethod())
			{
				writer.WriteLine();
				return;
			}
			if (CanInternalizeMethod())
			{
				writer.WriteLine("#if !{0} && !{1}", "FORCE_PINVOKE_INTERNAL", FORCE_PINVOKE_lib_INTERNAL(_pinvokeInfo.Module.Name));
			}
			writer.WriteLine("static {0} {1};", _context.Global.Services.Naming.ForPInvokeFunctionPointerTypedef(), _context.Global.Services.Naming.ForPInvokeFunctionPointerVariable());
			writer.WriteLine("if ({0} == NULL)", _context.Global.Services.Naming.ForPInvokeFunctionPointerVariable());
			writer.BeginBlock();
			string text = "parameterSize";
			writer.WriteLine("int {0} = {1};", text, CalculateParameterSize());
			string name = _pinvokeInfo.Module.Name;
			string entryPoint = _pinvokeInfo.EntryPoint;
			writer.WriteLine("{0} = il2cpp_codegen_resolve_pinvoke<{1}>(IL2CPP_NATIVE_STRING(\"{2}\"), \"{3}\", {4}, {5}, {6}, {7});", _context.Global.Services.Naming.ForPInvokeFunctionPointerVariable(), _context.Global.Services.Naming.ForPInvokeFunctionPointerTypedef(), name, entryPoint, GetIl2CppCallConvention(), GetCharSet(), text, _pinvokeInfo.IsNoMangle ? "true" : "false");
			writer.WriteLine("IL2CPP_ASSERT(" + _context.Global.Services.Naming.ForPInvokeFunctionPointerVariable() + " != NULL);");
			writer.EndBlock();
			if (CanInternalizeMethod())
			{
				writer.WriteLine("#endif");
			}
			writer.WriteLine();
		}

		private void EmitInternalAndExternalInvocation(IGeneratedMethodCodeWriter writer, string[] localVariableNames, string returnValueAssignment = "")
		{
			if (!CanInternalizeMethod())
			{
				writer.WriteLine("{0}{1};", returnValueAssignment, GetExternalMethodCallExpression(localVariableNames));
				return;
			}
			if (!ShouldInternalizeMethod())
			{
				writer.WriteLine("#if {0} || {1}", "FORCE_PINVOKE_INTERNAL", FORCE_PINVOKE_lib_INTERNAL(_pinvokeInfo.Module.Name));
			}
			writer.WriteLine("{0}{1};", returnValueAssignment, GetInternalizedMethodCallExpression(localVariableNames));
			if (!ShouldInternalizeMethod())
			{
				writer.WriteLine("#else");
				writer.WriteLine("{0}{1};", returnValueAssignment, GetExternalMethodCallExpression(localVariableNames));
				writer.WriteLine("#endif");
			}
		}

		protected override void WriteInteropCallStatement(IGeneratedMethodCodeWriter writer, string[] localVariableNames, IRuntimeMetadataAccess metadataAccess)
		{
			MethodReturnType methodReturnType = GetMethodReturnType();
			string returnValueAssignment = "";
			if (!PreserveSig())
			{
				returnValueAssignment = $"il2cpp_hresult_t {_context.Global.Services.Naming.ForInteropHResultVariable()} = ";
				if (methodReturnType.ReturnType.MetadataType != MetadataType.Void)
				{
					MarshalInfoWriterFor(methodReturnType).WriteNativeVariableDeclarationOfType(writer, _context.Global.Services.Naming.ForInteropReturnValue());
				}
			}
			else if (methodReturnType.ReturnType.MetadataType != MetadataType.Void)
			{
				returnValueAssignment = $"{MarshaledReturnType.DecoratedName} {_context.Global.Services.Naming.ForInteropReturnValue()} = ";
			}
			EmitInternalAndExternalInvocation(writer, localVariableNames, returnValueAssignment);
			if (_pinvokeInfo != null && _pinvokeInfo.SupportsLastError)
			{
				writer.WriteLine("il2cpp_codegen_marshal_store_last_error();");
			}
			if (!PreserveSig())
			{
				writer.WriteLine();
				writer.WriteStatement(Emit.Call("il2cpp_codegen_com_raise_exception_if_failed", _context.Global.Services.Naming.ForInteropHResultVariable(), "false"));
			}
		}

		private string GetPInvokeMethodVariable()
		{
			return $"{FormatReturnTypeForTypedef()} ({GetCallingConvention()} *{_context.Global.Services.Naming.ForPInvokeFunctionPointerTypedef()}) ({FormatParametersForTypedef()})";
		}

		private string GetCallingConvention()
		{
			if (_pinvokeInfo.IsCallConvStdCall)
			{
				return "STDCALL";
			}
			if (_pinvokeInfo.IsCallConvCdecl)
			{
				return "CDECL";
			}
			return "DEFAULT_CALL";
		}

		private string GetIl2CppCallConvention()
		{
			if (_pinvokeInfo.IsCallConvStdCall)
			{
				return "IL2CPP_CALL_STDCALL";
			}
			if (_pinvokeInfo.IsCallConvCdecl)
			{
				return "IL2CPP_CALL_C";
			}
			return "IL2CPP_CALL_DEFAULT";
		}

		private string GetCharSet()
		{
			if (_pinvokeInfo.IsCharSetNotSpec)
			{
				return "CHARSET_NOT_SPECIFIED";
			}
			if (_pinvokeInfo.IsCharSetAnsi)
			{
				return "CHARSET_ANSI";
			}
			return "CHARSET_UNICODE";
		}

		private string CalculateParameterSize()
		{
			MarshaledParameter[] parameters = Parameters;
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = 0; i < parameters.Length; i++)
			{
				if (i > 0)
				{
					stringBuilder.Append(" + ");
				}
				stringBuilder.Append(GetParameterSize(parameters[i]));
			}
			if (!PreserveSig() && InteropMethod.ReturnType.MetadataType != MetadataType.Void)
			{
				if (parameters.Length != 0)
				{
					stringBuilder.Append(" + ");
				}
				stringBuilder.Append("sizeof(void*)");
			}
			if (stringBuilder.Length <= 0)
			{
				return "0";
			}
			return stringBuilder.ToString();
		}

		private string GetParameterSize(MarshaledParameter parameter)
		{
			DefaultMarshalInfoWriter defaultMarshalInfoWriter = MarshalInfoWriterFor(parameter);
			if (defaultMarshalInfoWriter.NativeSize == "-1" && parameter.ParameterType.MetadataType != MetadataType.Array)
			{
				throw new NotSupportedException($"Cannot marshal parameter {parameter.NameInGeneratedCode} of type {parameter.ParameterType.FullName} for P/Invoke.");
			}
			MetadataType metadataType = parameter.ParameterType.MetadataType;
			if (metadataType == MetadataType.Class || metadataType == MetadataType.Array)
			{
				return "sizeof(void*)";
			}
			int num = 4 - defaultMarshalInfoWriter.NativeSizeWithoutPointers % 4;
			if (num != 4)
			{
				return defaultMarshalInfoWriter.NativeSize + " + " + num;
			}
			return defaultMarshalInfoWriter.NativeSize;
		}

		private string GetInternalizedMethodCallExpression(string[] localVariableNames)
		{
			string functionCallParametersExpression = GetFunctionCallParametersExpression(localVariableNames, !PreserveSig());
			return $"reinterpret_cast<{_context.Global.Services.Naming.ForPInvokeFunctionPointerTypedef()}>({_pinvokeInfo.EntryPoint})({functionCallParametersExpression})";
		}

		private string GetExternalMethodCallExpression(string[] localVariableNames)
		{
			string functionCallParametersExpression = GetFunctionCallParametersExpression(localVariableNames, !PreserveSig());
			return $"{_context.Global.Services.Naming.ForPInvokeFunctionPointerVariable()}({functionCallParametersExpression})";
		}

		protected string FormatReturnTypeForTypedef()
		{
			if (PreserveSig())
			{
				return MarshaledReturnType.DecoratedName;
			}
			return "il2cpp_hresult_t";
		}

		protected string FormatParametersForTypedef()
		{
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = 0; i < MarshaledParameterTypes.Length; i++)
			{
				if (stringBuilder.Length > 0)
				{
					stringBuilder.Append(", ");
				}
				stringBuilder.Append(MarshaledParameterTypes[i].DecoratedName);
			}
			if (!PreserveSig() && _methodDefinition.ReturnType.MetadataType != MetadataType.Void)
			{
				if (stringBuilder.Length > 0)
				{
					stringBuilder.Append(", ");
				}
				stringBuilder.Append(MarshaledReturnType.DecoratedName);
				stringBuilder.Append('*');
			}
			return stringBuilder.ToString();
		}

		private bool CanInternalizeMethod()
		{
			return _pinvokeInfo != null;
		}

		private bool ShouldInternalizeMethod()
		{
			if (!CanInternalizeMethod())
			{
				return false;
			}
			if (IsMethodExplicitlyMarkedInternal())
			{
				return true;
			}
			if (_internalizedMethods.TryGetValue(_pinvokeInfo.Module.Name, out var value))
			{
				return value.Any((string a) => a == _methodDefinition.Name);
			}
			return false;
		}

		private bool IsMethodExplicitlyMarkedInternal()
		{
			if (_pinvokeInfo.Module.Name == "__Internal")
			{
				return true;
			}
			return false;
		}

		private bool PreserveSig()
		{
			if (_methodDefinition.HasPInvokeInfo)
			{
				return _methodDefinition.IsPreserveSig;
			}
			return true;
		}
	}
}
