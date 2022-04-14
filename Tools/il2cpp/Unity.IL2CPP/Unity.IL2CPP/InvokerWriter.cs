using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using Unity.Cecil.Awesome;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Metadata;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP
{
	internal class InvokerWriter
	{
		public static TableInfo Write(GlobalReadOnlyContext context, IGeneratedCodeWriter writer, ReadOnlyInvokerCollection invokers)
		{
			foreach (InvokerSignature invoker in invokers.GetInvokers())
			{
				TypeReference typeReference = invoker.ReducedParameterTypes[0];
				writer.AddIncludeOrExternForTypeDefinition(typeReference.Module.TypeSystem.Object);
				writer.AddIncludeOrExternForTypeDefinition(typeReference);
				for (int i = 1; i < invoker.ReducedParameterTypes.Length; i++)
				{
					writer.AddIncludeOrExternForTypeDefinition(invoker.ReducedParameterTypes[i]);
				}
				writer.WriteLine("void* " + InvokerCollection.NameForInvoker(writer.Context, invoker) + " (Il2CppMethodPointer methodPointer, const RuntimeMethod* methodMetadata, void* obj, void** args)");
				writer.BeginBlock();
				WriteInvokerBody(writer, invoker.HasThis, invoker.ReducedParameterTypes, typeReference);
				writer.EndBlock();
				writer.WriteLine();
			}
			return writer.WriteTable("const InvokerMethod", context.Services.Naming.ForMetadataGlobalVar("g_Il2CppInvokerPointers"), invokers.GetInvokers(), (InvokerSignature item) => InvokerCollection.NameForInvoker(writer.Context, item), externTable: true);
		}

		private static void WriteInvokerBody(ICppCodeWriter writer, bool hasThis, IList<TypeReference> data, TypeReference returnType)
		{
			List<string> list = new List<string>(data.Count + 2);
			if (hasThis)
			{
				list.Add("void* obj");
			}
			for (int i = 1; i < data.Count; i++)
			{
				list.Add(writer.Context.Global.Services.Naming.ForVariable(data[i]) + " p" + i);
			}
			if (returnType.IsNotVoid() && writer.Context.Global.Parameters.ReturnAsByRefParameter)
			{
				list.Add(writer.Context.Global.Services.Naming.ForVariable(returnType.MakePointerType()) + " il2cppRetVal");
			}
			list.Add("const RuntimeMethod* method");
			writer.WriteStatement("typedef " + MethodSignatureWriter.FormatReturnType(writer.Context, returnType) + " (*Func)(" + list.AggregateWithComma() + ")");
			if (returnType.IsNotVoid())
			{
				writer.Write(writer.Context.Global.Services.Naming.ForVariable(returnType));
				writer.WriteLine(" ret;");
				if (!writer.Context.Global.Parameters.ReturnAsByRefParameter)
				{
					writer.Write("ret = ");
				}
			}
			List<string> list2 = new List<string>(data.Count + 2);
			if (hasThis)
			{
				list2.Add("obj");
			}
			for (int j = 1; j < data.Count; j++)
			{
				list2.Add(LoadParameter(writer.Context, data[j], "args[" + (j - 1) + "]", j - 1));
			}
			if (returnType.IsNotVoid() && writer.Context.Global.Parameters.ReturnAsByRefParameter)
			{
				list2.Add("&ret");
			}
			list2.Add("methodMetadata");
			writer.WriteLine("((Func)methodPointer)(" + list2.AggregateWithComma() + ");");
			writer.Write("return ");
			if (returnType.MetadataType == MetadataType.Void)
			{
				writer.Write("NULL");
			}
			else
			{
				writer.Write(returnType.IsValueType() ? "Box(il2cpp_codegen_class_from_type (il2cpp_codegen_method_return_type(methodMetadata)), &ret)" : "ret");
			}
			writer.WriteLine(";");
		}

		private static string LoadParameter(ReadOnlyContext context, TypeReference type, string param, int index)
		{
			if (type.IsByReference)
			{
				return Emit.Cast(context, type, param);
			}
			if (type.MetadataType == MetadataType.SByte || type.MetadataType == MetadataType.Byte || type.MetadataType == MetadataType.Boolean || type.MetadataType == MetadataType.Int16 || type.MetadataType == MetadataType.UInt16 || type.MetadataType == MetadataType.Char || type.MetadataType == MetadataType.Int32 || type.MetadataType == MetadataType.UInt32 || type.MetadataType == MetadataType.Int64 || type.MetadataType == MetadataType.UInt64 || type.MetadataType == MetadataType.IntPtr || type.MetadataType == MetadataType.UIntPtr || type.MetadataType == MetadataType.Single || type.MetadataType == MetadataType.Double)
			{
				return "*((" + context.Global.Services.Naming.ForVariable(new PointerType(type)) + ")" + param + ")";
			}
			if ((type.MetadataType == MetadataType.String || type.MetadataType == MetadataType.Class || type.MetadataType == MetadataType.Array || type.MetadataType == MetadataType.Pointer || type.MetadataType == MetadataType.Object) && !type.IsValueType())
			{
				return Emit.Cast(context, type, param);
			}
			if (type.MetadataType == MetadataType.GenericInstance && !type.IsValueType())
			{
				return Emit.Cast(context, type, param);
			}
			if (!type.IsValueType())
			{
				throw new Exception();
			}
			if (type.IsEnum())
			{
				return LoadParameter(context, type.GetUnderlyingEnumType(), param, index);
			}
			return "*((" + context.Global.Services.Naming.ForVariable(new PointerType(type)) + ")" + param + ")";
		}
	}
}
