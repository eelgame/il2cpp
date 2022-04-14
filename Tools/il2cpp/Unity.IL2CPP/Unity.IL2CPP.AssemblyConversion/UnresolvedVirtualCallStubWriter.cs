using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using Unity.Cecil.Awesome;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Metadata.RuntimeTypes;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.AssemblyConversion
{
	internal class UnresolvedVirtualCallStubWriter
	{
		public static UnresolvedVirtualsTablesInfo WriteUnresolvedStubs(SourceWritingContext context)
		{
			using (IGeneratedCodeWriter generatedCodeWriter = context.CreateProfiledGeneratedCodeSourceWriterInOutputDirectory("UnresolvedVirtualCallStubs.cpp"))
			{
				ReadOnlyCollection<KeyValuePair<IIl2CppRuntimeType[], uint>> sortedItems = context.Global.Results.SecondaryWritePart1.VirtualCalls.SortedItems;
				foreach (KeyValuePair<IIl2CppRuntimeType[], uint> item in sortedItems)
				{
					generatedCodeWriter.WriteLine(GetMethodSignature(generatedCodeWriter, item.Key, item.Value));
					generatedCodeWriter.WriteLine("{");
					generatedCodeWriter.Indent();
					generatedCodeWriter.WriteLine("il2cpp_codegen_raise_execution_engine_exception(method);");
					generatedCodeWriter.WriteLine("il2cpp_codegen_no_return();");
					generatedCodeWriter.Dedent();
					generatedCodeWriter.WriteLine("}");
					generatedCodeWriter.WriteLine();
				}
				UnresolvedVirtualsTablesInfo result = default(UnresolvedVirtualsTablesInfo);
				result.MethodPointersInfo = generatedCodeWriter.WriteArrayInitializer("const Il2CppMethodPointer", context.Global.Services.Naming.ForMetadataGlobalVar("g_UnresolvedVirtualMethodPointers"), sortedItems.Select(MethodNameFor), externArray: true, nullTerminate: false);
				result.SignatureTypes = context.Global.Results.SecondaryWritePart1.VirtualCalls.SortedKeys;
				return result;
			}
		}

		private static string GetMethodSignature(IGeneratedCodeWriter writer, IIl2CppRuntimeType[] signature, uint index)
		{
			RecordIncludes(writer, signature);
			return MethodSignatureWriter.GetMethodSignature($"UnresolvedVirtualCall_{index}", MethodSignatureWriter.FormatReturnType(writer.Context, signature[0].Type), FormatParameters(writer.Context, signature[0], signature.Skip(1)), "static");
		}

		private static string FormatParameters(ReadOnlyContext context, IIl2CppRuntimeType returnType, IEnumerable<IIl2CppRuntimeType> inputParams)
		{
			return ParametersFor(context, returnType, inputParams).ToList().AggregateWithComma();
		}

		private static IEnumerable<string> ParametersFor(ReadOnlyContext context, IIl2CppRuntimeType returnType, IEnumerable<IIl2CppRuntimeType> inputParams)
		{
			yield return FormatParameterName(context, context.Global.Services.TypeProvider.SystemObject, "__this");
			int i = 1;
			foreach (IIl2CppRuntimeType inputParam in inputParams)
			{
				yield return FormatParameterName(context, inputParam.Type, context.Global.Services.Naming.ForParameterName(inputParam.Type, i++));
			}
			if (returnType.Type.IsNotVoid() && context.Global.Parameters.ReturnAsByRefParameter)
			{
				PointerType parameterType = returnType.Type.MakePointerType();
				yield return FormatParameterName(context, parameterType, "il2cppRetVal");
			}
			yield return "const RuntimeMethod* method";
		}

		private static void RecordIncludes(IGeneratedCodeWriter writer, IIl2CppRuntimeType[] signature)
		{
			if (signature[0].Type.IsNotVoid())
			{
				writer.AddIncludesForTypeReference(signature[0].Type);
			}
			foreach (IIl2CppRuntimeType item in signature.Skip(1))
			{
				writer.AddIncludesForTypeReference(item.Type, requiresCompleteType: true);
			}
		}

		private static string MethodNameFor(KeyValuePair<IIl2CppRuntimeType[], uint> kvp)
		{
			return $"(const Il2CppMethodPointer) UnresolvedVirtualCall_{kvp.Value}";
		}

		private static string FormatParameterName(ReadOnlyContext context, TypeReference parameterType, string parameterName)
		{
			return string.Concat(string.Concat(string.Empty + context.Global.Services.Naming.ForVariable(parameterType), " "), parameterName);
		}
	}
}
