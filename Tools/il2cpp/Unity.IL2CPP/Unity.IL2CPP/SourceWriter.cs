using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using NiceIO;
using Unity.Cecil.Awesome;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Marshaling;
using Unity.IL2CPP.Marshaling.BodyWriters.NativeToManaged;
using Unity.IL2CPP.Metadata;

namespace Unity.IL2CPP
{
	public static class SourceWriter
	{
		public static void WriteEqualSizedChunks<T>(SourceWritingContext context, IEnumerable<T> items, string fileName, long chunkSize, Action<IGeneratedCodeWriter> writeHeaderAction, Action<IGeneratedCodeWriter, T, NPath> writeItemAction, Action<IGeneratedCodeWriter> writeFooterAction, Action<NPath, IGeneratedCodeWriter> writeEnd = null)
		{
			if (!items.Any())
			{
				return;
			}
			NPath outputDir = context.Global.InputData.OutputDir;
			NPath nPath = outputDir.Combine(fileName + ".cpp");
			InMemoryCodeWriter inMemoryCodeWriter = new InMemoryCodeWriter(context);
			writeHeaderAction(inMemoryCodeWriter);
			List<Tuple<NPath, IGeneratedCodeWriter>> list = new List<Tuple<NPath, IGeneratedCodeWriter>>
			{
				new Tuple<NPath, IGeneratedCodeWriter>(nPath, inMemoryCodeWriter)
			};
			foreach (T item in items)
			{
				if (inMemoryCodeWriter.Writer.BaseStream.Length > chunkSize)
				{
					writeFooterAction(inMemoryCodeWriter);
					nPath = outputDir.Combine($"{fileName}{list.Count}.cpp");
					inMemoryCodeWriter = new InMemoryCodeWriter(context);
					writeHeaderAction(inMemoryCodeWriter);
					list.Add(new Tuple<NPath, IGeneratedCodeWriter>(nPath, inMemoryCodeWriter));
				}
				writeItemAction(inMemoryCodeWriter, item, nPath);
			}
			writeFooterAction(inMemoryCodeWriter);
			foreach (Tuple<NPath, IGeneratedCodeWriter> item2 in list)
			{
				string text;
				using (IGeneratedCodeWriter generatedCodeWriter = context.CreateProfiledGeneratedCodeSourceWriter(item2.Item1))
				{
					generatedCodeWriter.Write(item2.Item2);
					writeEnd?.Invoke(item2.Item1, generatedCodeWriter);
					text = item2.Item1;
				}
				context.Global.Collectors.Symbols.CollectLineNumberInformation(context, text);
				item2.Item2.Dispose();
			}
		}

		internal static void WriteGenericMethodDefinition(SourceWritingContext context, IGeneratedMethodCodeWriter writer, GenericInstanceMethod method)
		{
			writer.AddIncludeForTypeDefinition(method.DeclaringType);
			MethodWriter.WriteMethodDefinition(context.CreateAssemblyWritingContext(method), writer, method, addToMethodCollection: false);
			WriterReversePInvokeWrapperForMethodIfNecessary(writer, method);
		}

		internal static void WriteType(SourceWritingContext context, IGeneratedMethodCodeWriter writer, TypeReference type, NPath filePath, bool writeMarshalingDefinitions, bool addToMethodCollection)
		{
			writer.AddStdInclude("limits");
			TypeDefinition systemArray = context.Global.Services.TypeProvider.SystemArray;
			if (systemArray != null)
			{
				writer.AddIncludeForTypeDefinition(systemArray);
			}
			writer.WriteClangWarningDisables();
			writer.AddIncludeForTypeDefinition(type);
			if (context.Global.Parameters.UsingTinyBackend)
			{
				TypeDefinitionWriter.WriteStaticFieldDefinitionsForTinyProfile(writer, type);
				TypeDefinitionWriter.WriteStaticFieldRVAExternsForTinyProfile(writer, type);
			}
			try
			{
				TypeDefinition typeDefinition = type.Resolve();
				if (writeMarshalingDefinitions)
				{
					WriteMarshalingDefinitions(context, writer, type);
				}
				foreach (MethodDefinition item in typeDefinition.Methods.Where((MethodDefinition m) => !m.HasGenericParameters))
				{
					context.Global.Services.ErrorInformation.CurrentMethod = item;
					if (context.Global.Parameters.EnableErrorMessageTest)
					{
						ErrorTypeAndMethod.ThrowIfIsErrorMethod(context, item);
					}
					if (!string.IsNullOrEmpty(context.Global.InputData.AssemblyMethod) && filePath != null && item.FullName.Contains(context.Global.InputData.AssemblyMethod))
					{
						context.Global.Collectors.MatchedAssemblyMethodSourceFiles.Add(filePath);
					}
					MethodReference method = item;
					if (type is GenericInstanceType genericInstanceType)
					{
						method = VTableBuilder.CloneMethodReference(genericInstanceType, item);
					}
					MethodWriter.WriteMethodDefinition(context.CreateAssemblyWritingContext(item), writer, method, addToMethodCollection);
				}
			}
			catch (Exception)
			{
				writer.ErrorOccurred = true;
				throw;
			}
			writer.WriteClangWarningEnables();
		}

		internal static void WriteMarshalingDefinitions(SourceWritingContext context, IGeneratedMethodCodeWriter writer, TypeReference type)
		{
			TypeDefinition typeDefinition = type.Resolve();
			context.Global.Services.ErrorInformation.CurrentType = typeDefinition;
			MarshalType[] marshalTypesForMarshaledType = MarshalingUtils.GetMarshalTypesForMarshaledType(context, type);
			foreach (MarshalType marshalType in marshalTypesForMarshaledType)
			{
				MarshalDataCollector.MarshalInfoWriterFor(context, type, marshalType).WriteMarshalFunctionDefinitions(writer);
			}
			TypeResolver typeResolver = TypeResolver.For(type);
			foreach (MethodDefinition method in typeDefinition.Methods)
			{
				MethodReference methodReference = typeResolver.Resolve(method);
				context.Global.Services.ErrorInformation.CurrentMethod = method;
				if (!methodReference.HasGenericParameters)
				{
					MethodWriter.WriteMethodForDelegatePInvokeIfNeeded(context, writer, methodReference);
				}
				WriterReversePInvokeWrapperForMethodIfNecessary(writer, methodReference);
			}
		}

		private static void WriterReversePInvokeWrapperForMethodIfNecessary(IGeneratedMethodCodeWriter writer, MethodReference method)
		{
			if (ReversePInvokeMethodBodyWriter.IsReversePInvokeWrapperNecessary(writer.Context, method) || writer.Context.Global.Parameters.EmitReversePInvokeWrapperDebuggingHelpers)
			{
				MethodWriter.WriteReversePInvokeMethodDefinition(writer, method);
			}
		}

		internal static void WriteTypeDefinition(SourceWritingContext context, IGeneratedCodeWriter writer, TypeReference type)
		{
			writer.AddStdInclude("stdint.h");
			writer.AddStdInclude("limits");
			if (type.IsSystemArray())
			{
				writer.WriteLine("struct Il2CppArrayBounds;");
			}
			if (!type.IsComOrWindowsRuntimeInterface(context))
			{
				new TypeDefinitionWriter().WriteTypeDefinitionFor(context, type, writer);
			}
			else
			{
				new ComInterfaceWriter(writer).WriteComInterfaceFor(type);
			}
		}
	}
}
