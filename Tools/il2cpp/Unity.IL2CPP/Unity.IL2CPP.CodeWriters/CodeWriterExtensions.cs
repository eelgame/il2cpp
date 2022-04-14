using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Unity.Cecil.Awesome;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.Metadata;
using Unity.IL2CPP.Metadata.RuntimeTypes;
using Unity.IL2CPP.Naming;
using Unity.IL2CPP.Tiny;

namespace Unity.IL2CPP.CodeWriters
{
	public static class CodeWriterExtensions
	{
		public static bool WriteWriteBarrierIfNeeded(this ICodeWriter writer, TypeReference valueType, string addressExpression, string valueExpression, bool alreadyHasBarrierOnObject = false)
		{
			if (!valueType.IsPointer)
			{
				if (!valueType.IsValueType())
				{
					if (alreadyHasBarrierOnObject)
					{
						writer.WriteLine("#if IL2CPP_ENABLE_STRICT_WRITE_BARRIERS");
					}
					writer.WriteLine("Il2CppCodeGenWriteBarrier((void**){0}, (void*){1});", addressExpression, valueExpression);
					if (alreadyHasBarrierOnObject)
					{
						writer.WriteLine("#endif");
					}
					return true;
				}
				if (valueType.Resolve().IsValueType && !valueType.IsPrimitive)
				{
					TypeDefinition typeDefinition = valueType.Resolve();
					if (typeDefinition.HasFields)
					{
						foreach (FieldDefinition field2 in typeDefinition.Fields)
						{
							if (!field2.IsStatic)
							{
								TypeResolver typeResolver = TypeResolver.For(valueType);
								FieldReference field = typeResolver.Resolve(field2);
								TypeReference valueType2 = typeResolver.ResolveFieldType(field);
								alreadyHasBarrierOnObject = writer.WriteWriteBarrierIfNeeded(valueType2, "&((" + addressExpression + ")->" + writer.Context.Global.Services.Naming.ForField(field2) + ")", "NULL", alreadyHasBarrierOnObject);
							}
						}
						return alreadyHasBarrierOnObject;
					}
				}
			}
			return alreadyHasBarrierOnObject;
		}

		public static T WriteIfNotEmpty<T>(this IGeneratedMethodCodeWriter writer, Action<IGeneratedMethodCodeWriter> writePrefixIfNotEmpty, Func<IGeneratedMethodCodeWriter, T> writeContent, Action<IGeneratedMethodCodeWriter> writePostfixIfNotEmpty)
		{
			using (InMemoryGeneratedMethodCodeWriter inMemoryGeneratedMethodCodeWriter = new InMemoryGeneratedMethodCodeWriter(writer.Context))
			{
				using (InMemoryGeneratedMethodCodeWriter inMemoryGeneratedMethodCodeWriter2 = new InMemoryGeneratedMethodCodeWriter(writer.Context))
				{
					inMemoryGeneratedMethodCodeWriter.Indent(writer.IndentationLevel);
					writePrefixIfNotEmpty(inMemoryGeneratedMethodCodeWriter);
					inMemoryGeneratedMethodCodeWriter2.Indent(inMemoryGeneratedMethodCodeWriter.IndentationLevel);
					T result = writeContent(inMemoryGeneratedMethodCodeWriter2);
					inMemoryGeneratedMethodCodeWriter2.Dedent(inMemoryGeneratedMethodCodeWriter.IndentationLevel);
					inMemoryGeneratedMethodCodeWriter.Dedent(writer.IndentationLevel);
					inMemoryGeneratedMethodCodeWriter2.Writer.Flush();
					if (inMemoryGeneratedMethodCodeWriter2.Writer.BaseStream.Length > 0)
					{
						inMemoryGeneratedMethodCodeWriter.Writer.Flush();
						writer.Write(inMemoryGeneratedMethodCodeWriter);
						writer.Write(inMemoryGeneratedMethodCodeWriter2);
						int num = inMemoryGeneratedMethodCodeWriter.IndentationLevel + inMemoryGeneratedMethodCodeWriter2.IndentationLevel;
						if (num > 0)
						{
							writer.Indent(num);
						}
						else if (num < 0)
						{
							writer.Dedent(-num);
						}
						writePostfixIfNotEmpty?.Invoke(writer);
					}
					return result;
				}
			}
		}

		public static void WriteIfNotEmpty(this IGeneratedMethodCodeWriter writer, Action<IGeneratedMethodCodeWriter> writePrefixIfNotEmpty, Action<IGeneratedMethodCodeWriter> writeContent, Action<IGeneratedMethodCodeWriter> writePostfixIfNotEmpty)
		{
			writer.WriteIfNotEmpty(writePrefixIfNotEmpty, (Func<IGeneratedMethodCodeWriter, object>)delegate(IGeneratedMethodCodeWriter bodyWriter)
			{
				writeContent(bodyWriter);
				return null;
			}, writePostfixIfNotEmpty);
		}

		public static void WriteMethodWithMetadataInitialization(this IGeneratedMethodCodeWriter writer, string methodSignature, string methodFullName, Action<IGeneratedMethodCodeWriter, IRuntimeMetadataAccess> writeMethodBody, string uniqueIdentifier, MethodReference methodRef)
		{
			string identifier = uniqueIdentifier + "_MetadataUsageId";
			MethodMetadataUsage methodMetadataUsage = new MethodMetadataUsage();
			MethodUsage methodUsage = new MethodUsage();
			using (InMemoryGeneratedMethodCodeWriter inMemoryGeneratedMethodCodeWriter2 = new InMemoryGeneratedMethodCodeWriter(writer.Context))
			{
				using (InMemoryGeneratedMethodCodeWriter inMemoryGeneratedMethodCodeWriter = new InMemoryGeneratedMethodCodeWriter(writer.Context))
				{
					inMemoryGeneratedMethodCodeWriter.Indent(writer.IndentationLevel + 1);
					inMemoryGeneratedMethodCodeWriter2.Indent(writer.IndentationLevel + 1);
					writeMethodBody(inMemoryGeneratedMethodCodeWriter, writer.GetDefaultRuntimeMetadataAccess(methodRef, methodMetadataUsage, methodUsage));
					if (methodMetadataUsage.UsesMetadata && !writer.Context.Global.Parameters.UsingTinyBackend)
					{
						WriteMethodMetadataInitialization(writer.Context, inMemoryGeneratedMethodCodeWriter2, identifier, methodMetadataUsage);
					}
					inMemoryGeneratedMethodCodeWriter.Dedent(writer.IndentationLevel + 1);
					inMemoryGeneratedMethodCodeWriter2.Dedent(writer.IndentationLevel + 1);
					foreach (MethodReference method in methodUsage.GetMethods())
					{
						writer.AddIncludeForMethodDeclaration(method);
					}
					if (methodMetadataUsage.UsesMetadata)
					{
						WriteMethodMetadataInitializationDeclarations(writer.Context, writer, identifier, methodMetadataUsage.GetIl2CppTypes(), methodMetadataUsage.GetTypeInfos(), methodMetadataUsage.GetInflatedMethods(), methodMetadataUsage.GetFieldInfos(), from s in methodMetadataUsage.GetStringLiterals()
							select s.Literal);
					}
					if (writer.Context.Global.Parameters.UsingTinyBackend)
					{
						foreach (MethodReference method2 in methodUsage.GetMethods())
						{
							writer.AddForwardDeclaration("IL2CPP_EXTERN_C const RuntimeMethod " + writer.Context.Global.Services.Naming.ForRuntimeMethodInfo(method2));
						}
					}
					using (new OptimizationWriter(writer, methodFullName))
					{
						writer.WriteLine(methodSignature);
						using (new BlockWriter(writer))
						{
							writer.Write(inMemoryGeneratedMethodCodeWriter2);
							writer.Write(inMemoryGeneratedMethodCodeWriter);
						}
					}
				}
			}
			if (methodMetadataUsage.UsesMetadata)
			{
				writer.AddMetadataUsage(identifier, methodMetadataUsage);
			}
		}

		public static void AddCurrentCodeGenModuleForwardDeclaration(this ICppCodeWriter writer, ReadOnlyContext context)
		{
			string text = context.Global.Services.Naming.ForCurrentCodeGenModuleVar();
			if (text != null)
			{
				writer.AddForwardDeclaration("IL2CPP_EXTERN_C_CONST Il2CppCodeGenModule " + text);
			}
		}

		private static void WriteMethodMetadataInitialization(ReadOnlyContext context, ICppCodeWriter writer, string identifier, MethodMetadataUsage metadataUsage)
		{
			INamingService naming = context.Global.Services.Naming;
			List<string> list = new List<string>(metadataUsage.UsageCount);
			list.AddRange(metadataUsage.GetTypeInfosNeedingInit().Select((Func<IIl2CppRuntimeType, string>)naming.ForRuntimeTypeInfo));
			list.AddRange(metadataUsage.GetIl2CppTypesNeedingInit().Select((Func<IIl2CppRuntimeType, string>)naming.ForRuntimeIl2CppType));
			list.AddRange(metadataUsage.GetInflatedMethodsNeedingInit().Select((Func<MethodReference, string>)naming.ForRuntimeMethodInfo));
			list.AddRange(metadataUsage.GetFieldInfosNeedingInit().Select((Func<Il2CppRuntimeFieldReference, string>)naming.ForRuntimeFieldInfo));
			list.AddRange(from s in metadataUsage.GetStringLiteralsNeedingInit()
				select naming.ForRuntimeUniqueStringLiteralIdentifier(s.Literal));
			if (list.Count == 0)
			{
				return;
			}
			string text;
			if (context.Global.Parameters.EnableReload)
			{
				text = naming.ForReloadMethodMetadataInitialized() + "[" + identifier + "]";
			}
			else
			{
				text = "s_Il2CppMethodInitialized";
				writer.WriteStatement("static bool " + text);
			}
			writer.WriteLine("if (!{0})", text);
			writer.BeginBlock();
			foreach (string item in list.ToSortedCollection())
			{
				writer.WriteStatement("il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&" + item + ")");
			}
			writer.WriteStatement(Emit.Assign(text, "true"));
			writer.EndBlock();
		}

		public static void WriteMethodMetadataInitializationDeclarations(ReadOnlyContext context, ICppCodeWriter writer, string identifier, IEnumerable<IIl2CppRuntimeType> types, IEnumerable<IIl2CppRuntimeType> typeInfos, IEnumerable<MethodReference> methods, IEnumerable<Il2CppRuntimeFieldReference> fields, IEnumerable<string> stringLiterals)
		{
			foreach (IIl2CppRuntimeType type in types)
			{
				writer.AddForwardDeclaration("IL2CPP_EXTERN_C const RuntimeType* " + writer.Context.Global.Services.Naming.ForRuntimeIl2CppType(type));
			}
			foreach (IIl2CppRuntimeType typeInfo in typeInfos)
			{
				if (context.Global.Parameters.UsingTinyBackend)
				{
					writer.AddForwardDeclaration("IL2CPP_EXTERN_C const uint32_t " + context.Global.Services.Naming.TinyTypeOffsetNameFor(typeInfo.Type));
				}
				else
				{
					writer.AddForwardDeclaration("IL2CPP_EXTERN_C RuntimeClass* " + writer.Context.Global.Services.Naming.ForRuntimeTypeInfo(typeInfo));
				}
			}
			foreach (MethodReference method in methods)
			{
				writer.AddForwardDeclaration("IL2CPP_EXTERN_C const RuntimeMethod* " + writer.Context.Global.Services.Naming.ForRuntimeMethodInfo(method));
			}
			foreach (Il2CppRuntimeFieldReference field in fields)
			{
				writer.AddForwardDeclaration("IL2CPP_EXTERN_C RuntimeField* " + writer.Context.Global.Services.Naming.ForRuntimeFieldInfo(field));
			}
			foreach (string stringLiteral in stringLiterals)
			{
				writer.AddForwardDeclaration("IL2CPP_EXTERN_C String_t* " + writer.Context.Global.Services.Naming.ForRuntimeUniqueStringLiteralIdentifier(stringLiteral));
			}
			if (context.Global.Parameters.EnableReload)
			{
				writer.AddForwardDeclaration("IL2CPP_EXTERN_C const uint32_t " + identifier);
				writer.AddForwardDeclaration("IL2CPP_EXTERN_C bool " + context.Global.Services.Naming.ForReloadMethodMetadataInitialized() + "[];");
			}
		}

		public static IRuntimeMetadataAccess GetDefaultRuntimeMetadataAccess(this IGeneratedMethodCodeWriter writer, MethodReference method, MethodMetadataUsage methodMetadataUsage, MethodUsage methodUsage)
		{
			return writer.Context.Global.Services.Factory.GetDefaultRuntimeMetadataAccess(writer.Context, method, methodMetadataUsage, methodUsage);
		}

		public static void WriteReturnStatement(this IGeneratedMethodCodeWriter writer, string returnExpression = null)
		{
			if (string.IsNullOrEmpty(returnExpression))
			{
				writer.WriteStatement("return");
			}
			else
			{
				writer.WriteStatement("return " + returnExpression);
			}
		}

		public static void WriteManagedReturnStatement(this IGeneratedMethodCodeWriter writer, string returnExpression = null)
		{
			if (string.IsNullOrEmpty(returnExpression))
			{
				writer.WriteReturnStatement();
			}
			else if (writer.Context.Global.Parameters.ReturnAsByRefParameter)
			{
				writer.WriteStatement("*il2cppRetVal = " + returnExpression);
				writer.WriteReturnStatement();
			}
			else
			{
				writer.WriteReturnStatement(returnExpression);
			}
		}

		public static void WriteMethodCallStatement(this IGeneratedMethodCodeWriter writer, IRuntimeMetadataAccess metadataAccess, string thisVariableName, MethodReference callingMethod, MethodReference method, MethodCallType methodCallType, params string[] args)
		{
			WriteMethodCallStatementInternal(writer, metadataAccess, thisVariableName, callingMethod, method, methodCallType, null, args);
		}

		public static void WriteMethodCallWithResultStatement(this IGeneratedMethodCodeWriter writer, IRuntimeMetadataAccess metadataAccess, string thisVariableName, MethodReference callingMethod, MethodReference method, MethodCallType methodCallType, string returnVariable, params string[] args)
		{
			WriteMethodCallStatementInternal(writer, metadataAccess, thisVariableName, callingMethod, method, methodCallType, returnVariable, args);
		}

		private static void WriteMethodCallStatementInternal(IGeneratedMethodCodeWriter writer, IRuntimeMetadataAccess metadataAccess, string thisVariableName, MethodReference callingMethod, MethodReference method, MethodCallType methodCallType, string returnVariable, params string[] args)
		{
			List<string> list = new List<string>();
			if (method.HasThis)
			{
				list.Add(thisVariableName);
			}
			if (args.Length != 0)
			{
				list.AddRange(args);
			}
			VTableBuilder vTableBuilder = ((methodCallType == MethodCallType.Virtual) ? new VTableBuilder() : null);
			MethodBodyWriter.WriteMethodCallExpression(returnVariable, () => metadataAccess.HiddenMethodInfo(method), writer, callingMethod, method, method, TypeResolver.Empty, methodCallType, metadataAccess, vTableBuilder, list, useArrayBoundsCheck: false);
		}

		public static void WriteClangWarningDisables(this ICodeWriter writer)
		{
			writer.Writer.WriteClangWarningDisables();
		}

		public static void WriteClangWarningEnables(this ICodeWriter writer)
		{
			writer.Writer.WriteClangWarningEnables();
		}

		public static void WriteClangWarningDisables(this TextWriter writer)
		{
			writer.WriteLine("#ifdef __clang__");
			writer.WriteLine("#pragma clang diagnostic push");
			writer.WriteLine("#pragma clang diagnostic ignored \"-Winvalid-offsetof\"");
			writer.WriteLine("#pragma clang diagnostic ignored \"-Wunused-variable\"");
			writer.WriteLine("#endif");
		}

		public static void WriteClangWarningEnables(this TextWriter writer)
		{
			writer.WriteLine("#ifdef __clang__");
			writer.WriteLine("#pragma clang diagnostic pop");
			writer.WriteLine("#endif");
		}

		public static void AddCodeGenMetadataIncludes(this ICppCodeWriter writer)
		{
			writer.AddInclude("il2cpp-config.h");
			writer.AddInclude("codegen/il2cpp-codegen-metadata.h");
		}

		public static TableInfo WriteArrayInitializer(this ICodeWriter writer, string type, string variableName, IEnumerable<string> values, bool externArray, bool nullTerminate = true)
		{
			values = (nullTerminate ? values.Concat(new string[1] { "NULL" }) : values);
			string[] array = values.ToArray();
			TableInfo result = new TableInfo(array.Length, type, variableName, externArray);
			if (externArray)
			{
				writer.WriteLine(result.GetDeclaration());
			}
			writer.WriteLine("{0} {1}[{2}] = ", type, variableName, (array.Length == 0) ? 1 : array.Length);
			writer.WriteFieldInitializer(array);
			return result;
		}

		public static TableInfo WriteArrayInitializer<T>(this ICodeWriter writer, string type, string variableName, ICollection<T> values, Func<T, string> map, bool externArray)
		{
			TableInfo result = new TableInfo(values.Count, type, variableName, externArray);
			if (externArray)
			{
				writer.WriteLine(result.GetDeclaration());
			}
			writer.WriteLine("{0} {1}[{2}] = ", type, variableName, (values.Count == 0) ? 1 : values.Count);
			writer.WriteFieldInitializer(values, map);
			return result;
		}

		public static void WriteStructInitializer(this ICodeWriter writer, string type, string variableName, IEnumerable<string> values, bool externStruct)
		{
			if (externStruct)
			{
				writer.WriteLine("IL2CPP_EXTERN_C {0} {1};", type, variableName);
			}
			writer.WriteLine("{0} {1} = ", type, variableName);
			writer.WriteFieldInitializer(values);
		}

		private static void WriteFieldInitializer(this ICodeWriter writer, IEnumerable<string> values)
		{
			writer.BeginBlock();
			foreach (string value in values)
			{
				writer.Write(value);
				writer.WriteLine(",");
			}
			writer.EndBlock(semicolon: true);
		}

		private static void WriteFieldInitializer<T>(this ICodeWriter writer, IEnumerable<T> values, Func<T, string> map)
		{
			writer.BeginBlock();
			foreach (T value in values)
			{
				writer.Write(map(value));
				writer.WriteLine(",");
			}
			writer.EndBlock(semicolon: true);
		}
	}
}
