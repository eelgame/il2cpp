using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Unity.Cecil.Awesome;
using Unity.Cecil.Awesome.Comparers;
using Unity.Cecil.Awesome.Ordering;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Results;
using Unity.IL2CPP.GenericSharing;
using Unity.IL2CPP.Metadata;
using Unity.IL2CPP.Metadata.RuntimeTypes;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP
{
	internal class CppDeclarationsWriter
	{
		public static void Write(StreamWriter writer, ICppDeclarationsBasic declarationsIn)
		{
			string[] includesToSkip = new string[3] { "\"il2cpp-config.h\"", "<alloca.h>", "<malloc.h>" };
			writer.WriteLine();
			foreach (string item in declarationsIn.Includes.Where((string i) => !includesToSkip.Contains(i) && i.StartsWith("<")))
			{
				writer.WriteLine("#include {0}", item);
			}
			writer.WriteLine();
			foreach (string item2 in declarationsIn.Includes.Where((string i) => !includesToSkip.Contains(i) && !i.StartsWith("<")))
			{
				writer.WriteLine("#include {0}", item2);
			}
			writer.WriteLine();
			writer.WriteLine();
			foreach (string item3 in declarationsIn.RawTypeForwardDeclarations.ToSortedCollection())
			{
				writer.WriteLine(item3 + ";");
			}
			writer.WriteLine();
			writer.WriteLine();
			foreach (string rawMethodForwardDeclaration in declarationsIn.RawMethodForwardDeclarations)
			{
				writer.WriteLine(rawMethodForwardDeclaration + ";");
			}
			writer.WriteLine();
			writer.Flush();
		}

		public static void Write(SourceWritingContext context, StreamWriter writer, ICppDeclarations declarationsIn)
		{
			string[] includesToSkip = new string[3] { "\"il2cpp-config.h\"", "<alloca.h>", "<malloc.h>" };
			ICppDeclarationsCache cppDeclarationsCache = context.Global.Results.CppDeclarationsCache;
			context.Global.Collectors.CppDeclarationsCache.PopulateCache(context, declarationsIn.TypeIncludes);
			HashSet<TypeReference> hashSet = new HashSet<TypeReference>(declarationsIn.TypeIncludes, new TypeReferenceEqualityComparer());
			ReadOnlyHashSet<TypeReference> dependencies = CppDeclarationsCollector.GetDependencies(declarationsIn.TypeIncludes, cppDeclarationsCache);
			hashSet.UnionWith(dependencies);
			ReadOnlyCollection<TypeReference> readOnlyCollection = hashSet.ToSortedCollection(new CppIncludeDepthComparer(context.Global.Collectors.CppIncludeDepthCalculatorCache));
			CppDeclarations cppDeclarations = new CppDeclarations();
			cppDeclarations.Add(declarationsIn);
			foreach (TypeReference item in readOnlyCollection)
			{
				cppDeclarations.Add(cppDeclarationsCache.GetDeclarations(item));
			}
			writer.WriteLine();
			foreach (string rawFileLevelPreprocessorStmt in cppDeclarations.RawFileLevelPreprocessorStmts)
			{
				writer.WriteLine(rawFileLevelPreprocessorStmt);
			}
			writer.WriteLine();
			foreach (string item2 in cppDeclarations.Includes.Where((string i) => !includesToSkip.Contains(i) && i.StartsWith("<")))
			{
				writer.WriteLine("#include {0}", item2);
			}
			writer.WriteLine();
			foreach (string item3 in cppDeclarations.Includes.Where((string i) => !includesToSkip.Contains(i) && !i.StartsWith("<")))
			{
				writer.WriteLine("#include {0}", item3);
			}
			writer.WriteLine();
			WriteVirtualMethodDeclaration(context, writer, cppDeclarations.VirtualMethods);
			writer.WriteLine();
			foreach (TypeReference item4 in cppDeclarations.ForwardDeclarations.ToSortedCollection())
			{
				if (!item4.IsSystemObject() && !item4.IsSystemArray())
				{
					if (context.Global.Parameters.EmitComments)
					{
						writer.WriteLine("// {0}", item4.FullName);
					}
					writer.WriteLine("struct {0};", context.Global.Services.Naming.ForType(item4));
				}
			}
			writer.WriteLine();
			foreach (string item5 in cppDeclarations.RawTypeForwardDeclarations.ToSortedCollection())
			{
				writer.WriteLine(item5 + ";");
			}
			writer.WriteLine();
			foreach (ArrayType item6 in cppDeclarations.ArrayTypes.ToSortedCollection())
			{
				writer.WriteLine("struct {0};", context.Global.Services.Naming.ForType(item6));
			}
			writer.WriteLine();
			writer.WriteLine("IL2CPP_EXTERN_C_BEGIN");
			foreach (IIl2CppRuntimeType typeExtern in cppDeclarations.TypeExterns)
			{
				writer.WriteLine("extern " + Il2CppTypeSupport.DeclarationFor(typeExtern.Type) + " " + context.Global.Services.Naming.ForIl2CppType(typeExtern) + ";");
			}
			foreach (IIl2CppRuntimeType[] genericInstExtern in cppDeclarations.GenericInstExterns)
			{
				writer.WriteLine("extern const Il2CppGenericInst " + context.Global.Services.Naming.ForGenericInst(genericInstExtern) + ";");
			}
			foreach (TypeReference genericClassExtern in cppDeclarations.GenericClassExterns)
			{
				writer.WriteLine("extern Il2CppGenericClass " + context.Global.Services.Naming.ForGenericClass(genericClassExtern) + ";");
			}
			writer.WriteLine("IL2CPP_EXTERN_C_END");
			writer.WriteLine();
			if (readOnlyCollection.Count > 0)
			{
				writer.WriteClangWarningDisables();
				foreach (TypeReference item7 in readOnlyCollection)
				{
					string source = cppDeclarationsCache.GetSource(item7);
					writer.Write(source);
				}
				writer.WriteClangWarningEnables();
			}
			foreach (ArrayType arrayType in cppDeclarations.ArrayTypes)
			{
				TypeDefinitionWriter.WriteArrayTypeDefinition(context, arrayType, new CodeWriter(context, writer));
			}
			writer.WriteLine();
			foreach (string rawMethodForwardDeclaration in cppDeclarations.RawMethodForwardDeclarations)
			{
				writer.WriteLine(rawMethodForwardDeclaration + ";");
			}
			writer.WriteLine();
			foreach (MethodReference sharedMethod in cppDeclarations.SharedMethods)
			{
				WriteSharedMethodDeclaration(context.CreateMethodWritingContext(sharedMethod), writer);
			}
			writer.WriteLine();
			foreach (MethodReference method in cppDeclarations.Methods)
			{
				WriteMethodDeclaration(context.CreateMethodWritingContext(method), writer);
			}
			foreach (string value in cppDeclarations.InternalPInvokeMethodDeclarations.Values)
			{
				writer.Write(value);
			}
			foreach (string value2 in cppDeclarations._internalPInvokeMethodDeclarationsForForcedInternalPInvoke.Values)
			{
				writer.Write(value2);
			}
			writer.Flush();
		}

		private static void WriteVirtualMethodDeclaration(SourceWritingContext context, StreamWriter writer, IEnumerable<VirtualMethodDeclarationData> virtualMethodDeclarationData)
		{
			CodeWriter writer2 = new CodeWriter(context, writer, owns: false);
			HashSet<InvokerData> hashSet = new HashSet<InvokerData>();
			HashSet<InvokerData> hashSet2 = new HashSet<InvokerData>();
			HashSet<InvokerData> hashSet3 = new HashSet<InvokerData>();
			HashSet<InvokerData> hashSet4 = new HashSet<InvokerData>();
			foreach (VirtualMethodDeclarationData virtualMethodDeclarationDatum in virtualMethodDeclarationData)
			{
				if (virtualMethodDeclarationDatum.DeclaringTypeIsInterface)
				{
					if (virtualMethodDeclarationDatum.HasGenericParameters)
					{
						hashSet4.Add(new InvokerData(virtualMethodDeclarationDatum.ReturnsVoid, virtualMethodDeclarationDatum.NumberOfParameters));
					}
					else
					{
						hashSet3.Add(new InvokerData(virtualMethodDeclarationDatum.ReturnsVoid, virtualMethodDeclarationDatum.NumberOfParameters));
					}
				}
				else if (virtualMethodDeclarationDatum.HasGenericParameters)
				{
					hashSet2.Add(new InvokerData(virtualMethodDeclarationDatum.ReturnsVoid, virtualMethodDeclarationDatum.NumberOfParameters));
				}
				else
				{
					hashSet.Add(new InvokerData(virtualMethodDeclarationDatum.ReturnsVoid, virtualMethodDeclarationDatum.NumberOfParameters));
				}
			}
			foreach (InvokerData item in hashSet)
			{
				InterfaceAndVirtualInvokeWriter.WriteVirtual(writer2, item);
			}
			foreach (InvokerData item2 in hashSet2)
			{
				InterfaceAndVirtualInvokeWriter.WriteGenericVirtual(writer2, item2);
			}
			foreach (InvokerData item3 in hashSet3)
			{
				InterfaceAndVirtualInvokeWriter.WriteInterface(writer2, item3);
			}
			foreach (InvokerData item4 in hashSet4)
			{
				InterfaceAndVirtualInvokeWriter.WriteGenericInterface(writer2, item4);
			}
		}

		private static void WriteSharedMethodDeclaration(MethodWriteContext context, StreamWriter writer)
		{
			MethodReference methodReference = context.MethodReference;
			GenericInstanceType genericInstanceType = methodReference.DeclaringType as GenericInstanceType;
			bool flag = methodReference.ShouldInline(context.Global.Parameters);
			if (genericInstanceType != null && GenericsUtilities.CheckForMaximumRecursion(context, genericInstanceType))
			{
				string text = (flag ? "_inline" : "");
				writer.WriteLine("{0};", MethodSignatureWriter.GetMethodSignature(context.Global.Services.Naming.ForMethodNameOnly(methodReference) + text, MethodSignatureWriter.FormatReturnType(context, context.ResolvedReturnType), MethodSignatureWriter.FormatParameters(context, methodReference, ParameterFormat.WithTypeAndName, includeHiddenMethodInfo: true), "inline", string.Empty));
				return;
			}
			if (!GenericSharingAnalysis.IsSharedMethod(context, methodReference))
			{
				throw new InvalidOperationException();
			}
			if (context.Global.Parameters.EmitComments)
			{
				writer.WriteLine("// {0}", methodReference.FullName);
			}
			if (flag)
			{
				writer.WriteLine("{0};", MethodSignatureWriter.GetSharedMethodSignatureRawInline(context));
			}
			else
			{
				writer.WriteLine("{0};", MethodSignatureWriter.GetSharedMethodSignatureRaw(context));
			}
		}

		private static void WriteMethodDeclaration(MethodWriteContext context, StreamWriter writer)
		{
			MethodReference methodReference = context.MethodReference;
			GenericInstanceType genericInstanceType = methodReference.DeclaringType as GenericInstanceType;
			bool flag = methodReference.ShouldInline(context.Global.Parameters);
			if (genericInstanceType != null && GenericsUtilities.CheckForMaximumRecursion(context, genericInstanceType))
			{
				string attributes = MethodSignatureWriter.BuildMethodAttributes(methodReference);
				string text = (flag ? "_inline" : "");
				writer.WriteLine("{0};", MethodSignatureWriter.GetMethodSignature(context.Global.Services.Naming.ForMethodNameOnly(methodReference) + text, MethodSignatureWriter.FormatReturnType(context, context.ResolvedReturnType), MethodSignatureWriter.FormatParameters(context, methodReference, ParameterFormat.WithTypeAndName, includeHiddenMethodInfo: true), "IL2CPP_EXTERN_C", attributes));
				return;
			}
			if (context.Global.Parameters.EmitComments)
			{
				writer.WriteLine("// {0}", methodReference.FullName);
			}
			if (GenericSharingAnalysis.CanShareMethod(context, methodReference))
			{
				string text2 = (flag ? "_inline" : "");
				writer.WriteLine("inline {0} {1} ({2})", MethodSignatureWriter.FormatReturnType(context, context.ResolvedReturnType), context.Global.Services.Naming.ForMethodNameOnly(methodReference) + text2, MethodSignatureWriter.FormatParameters(context, methodReference, ParameterFormat.WithTypeAndName, includeHiddenMethodInfo: true));
				writer.WriteLine("{");
				writer.Write("\t");
				if (!methodReference.ReturnType.IsVoid() && !context.Global.Parameters.ReturnAsByRefParameter)
				{
					writer.Write("return ");
				}
				writer.WriteLine("(({0}){1})({2});", MethodSignatureWriter.GetMethodPointer(context, methodReference), context.Global.Services.Naming.ForMethod(GenericSharingAnalysis.GetSharedMethod(context, methodReference)) + "_gshared" + text2, MethodSignatureWriter.FormatParameters(context, methodReference, ParameterFormat.WithName, includeHiddenMethodInfo: true));
				writer.WriteLine("}");
			}
			else if (flag)
			{
				writer.WriteLine(MethodSignatureWriter.GetMethodSignatureRawInline(context) + ";");
			}
			else
			{
				writer.WriteLine(MethodSignatureWriter.GetMethodSignatureRaw(context) + ";");
			}
		}
	}
}
