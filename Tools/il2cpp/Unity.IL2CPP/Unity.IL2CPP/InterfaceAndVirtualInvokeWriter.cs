using System;
using System.Collections.Generic;
using System.Linq;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;

namespace Unity.IL2CPP
{
	internal class InterfaceAndVirtualInvokeWriter
	{
		internal static void WriteGenericInterface(CodeWriter writer, InvokerData data)
		{
			Write(writer, data, "GenericInterface", delegate
			{
				writer.WriteLine("VirtualInvokeData invokeData;");
				writer.WriteLine("il2cpp_codegen_get_generic_interface_invoke_data(method, obj, &invokeData);");
			}, () => new string[2] { "const RuntimeMethod* method", "RuntimeObject* obj" });
		}

		internal static void WriteInterface(CodeWriter writer, InvokerData data)
		{
			string classTypeName = ((!writer.Context.Global.Parameters.UsingTinyBackend) ? "RuntimeClass" : "TinyType");
			Write(writer, data, "Interface", delegate
			{
				writer.WriteLine("const VirtualInvokeData& invokeData = il2cpp_codegen_get_interface_invoke_data(slot, obj, declaringInterface);");
			}, () => new string[3]
			{
				"Il2CppMethodSlot slot",
				classTypeName + "* declaringInterface",
				"RuntimeObject* obj"
			});
		}

		internal static void WriteGenericVirtual(CodeWriter writer, InvokerData data)
		{
			Write(writer, data, "GenericVirt", delegate
			{
				writer.WriteLine("VirtualInvokeData invokeData;");
				writer.WriteLine("il2cpp_codegen_get_generic_virtual_invoke_data(method, obj, &invokeData);");
			}, () => new string[2] { "const RuntimeMethod* method", "RuntimeObject* obj" });
		}

		internal static void WriteVirtual(CodeWriter writer, InvokerData data)
		{
			Write(writer, data, "Virt", delegate
			{
				writer.WriteLine("const VirtualInvokeData& invokeData = il2cpp_codegen_get_virtual_invoke_data(slot, obj);");
			}, () => new string[2] { "Il2CppMethodSlot slot", "RuntimeObject* obj" });
		}

		private static void Write(CodeWriter writer, InvokerData data, string prefix, Action writeRetrieveInvokeData, Func<string[]> getInvokeArgs)
		{
			string text = (data.VoidReturn ? "Action" : "Func");
			string text2 = TemplateParametersFor(data);
			if (!string.IsNullOrEmpty(text2))
			{
				writer.WriteLine("template <{0}>", text2);
			}
			writer.WriteLine($"struct {prefix}{text}Invoker{data.ParameterCount}");
			writer.BeginBlock();
			writer.WriteLine("typedef {0} (*{1})({2});", ReturnTypeFor(data), text, FunctionPointerParametersFor(writer.Context, data));
			writer.WriteLine();
			InvokerData invokerData = data;
			writer.WriteLine("static inline {0} Invoke ({1})", ReturnTypeFor(data), getInvokeArgs().Concat(Enumerable.Range(1, invokerData.ParameterCount).Select((int m, int i) => string.Format("T{0} p{0}", i + 1))).AggregateWithComma());
			writer.BeginBlock();
			writeRetrieveInvokeData();
			if (!writer.Context.Global.Parameters.UsingTinyBackend)
			{
				writer.WriteLine("{0}(({1})invokeData.methodPtr)({2});", data.VoidReturn ? "" : "return ", text, CallParametersFor(writer.Context, data));
			}
			else
			{
				writer.WriteLine("{0}(({1})invokeData)({2});", data.VoidReturn ? "" : "return ", text, CallParametersFor(writer.Context, data));
			}
			writer.EndBlock();
			writer.EndBlock(semicolon: true);
		}

		private static string ReturnTypeFor(InvokerData data)
		{
			if (!data.VoidReturn)
			{
				return "R";
			}
			return "void";
		}

		private static string TemplateParametersFor(InvokerData data)
		{
			if (data.VoidReturn && data.ParameterCount == 0)
			{
				return string.Empty;
			}
			IEnumerable<string> first;
			if (!data.VoidReturn)
			{
				IEnumerable<string> enumerable = new string[1] { "R" };
				first = enumerable;
			}
			else
			{
				first = Enumerable.Empty<string>();
			}
			return (from t in first.Concat(from i in Enumerable.Range(1, data.ParameterCount)
					select "T" + i)
				select "typename " + t).AggregateWithComma();
		}

		private static string CallParametersFor(ReadOnlyContext context, InvokerData data)
		{
			IEnumerable<string> enumerable = new string[1] { "obj" }.Concat(Enumerable.Range(1, data.ParameterCount).Select((int m, int i) => $"p{i + 1}"));
			if (!context.Global.Parameters.UsingTinyBackend)
			{
				enumerable = enumerable.Concat(new string[1] { "invokeData.method" });
			}
			return enumerable.AggregateWithComma();
		}

		private static string FunctionPointerParametersFor(ReadOnlyContext context, InvokerData data)
		{
			IEnumerable<string> enumerable = new string[1] { "void*" }.Concat(Enumerable.Range(1, data.ParameterCount).Select((int m, int i) => $"T{i + 1}"));
			if (!context.Global.Parameters.UsingTinyBackend)
			{
				enumerable = enumerable.Concat(new string[1] { "const RuntimeMethod*" });
			}
			return enumerable.AggregateWithComma();
		}
	}
}
