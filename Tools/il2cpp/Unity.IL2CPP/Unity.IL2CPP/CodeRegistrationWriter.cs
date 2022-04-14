using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using Mono.Cecil;
using Unity.Cecil.Awesome.Ordering;
using Unity.IL2CPP.AssemblyConversion.PrimaryCollection.Results;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.Marshaling;
using Unity.IL2CPP.Marshaling.BodyWriters.NativeToManaged;
using Unity.IL2CPP.Marshaling.MarshalInfoWriters;
using Unity.IL2CPP.Metadata;
using Unity.IL2CPP.Metadata.RuntimeTypes;
using Unity.IL2CPP.Naming;
using Unity.IL2CPP.WindowsRuntime;

namespace Unity.IL2CPP
{
	public static class CodeRegistrationWriter
	{
		private enum CodeRegistrationWriterMode
		{
			AllAssemblies,
			PerAssembly,
			PerAssemblyGlobal
		}

		public static void WriteCodeRegistration(SourceWritingContext context, ReadOnlyMethodTables methodPointerTables, UnresolvedVirtualsTablesInfo virtualCallTables, ReadOnlyInvokerCollection invokerCollection, ReadOnlyCollection<string> codeGenModules)
		{
			TableInfo reversePInvokeWrappersTable = WriteReversePInvokeWrappersTable(context);
			TableInfo genericMethodPointerTable = WriteGenericMethodPointerTable(context, methodPointerTables);
			TableInfo genericAdjustorThunkTable = WriteGenericAdjustorThunkTable(context, methodPointerTables);
			TableInfo invokerTable = WriteInvokerTable(context, invokerCollection);
			TableInfo interopDataTable = WriteInteropDataTable(context);
			TableInfo windowsRuntimeFactoryTable = WriteWindowsRuntimeFactoryTable(context);
			WriteCodeRegistration(context, invokerTable, reversePInvokeWrappersTable, genericMethodPointerTable, genericAdjustorThunkTable, virtualCallTables, interopDataTable, windowsRuntimeFactoryTable, codeGenModules, (context.Global.AsReadOnly().Services.ContextScope.UniqueIdentifier != null) ? CodeRegistrationWriterMode.PerAssembly : CodeRegistrationWriterMode.AllAssemblies);
		}

		public static void WritePerAssemblyGlobalCodeRegistration(SourceWritingContext context, ReadOnlyCollection<string> codeGenModules)
		{
			TableInfo empty = TableInfo.Empty;
			TableInfo empty2 = TableInfo.Empty;
			TableInfo empty3 = TableInfo.Empty;
			TableInfo empty4 = TableInfo.Empty;
			TableInfo empty5 = TableInfo.Empty;
			TableInfo empty6 = TableInfo.Empty;
			UnresolvedVirtualsTablesInfo unresolvedVirtualsTablesInfo = default(UnresolvedVirtualsTablesInfo);
			unresolvedVirtualsTablesInfo.MethodPointersInfo = TableInfo.Empty;
			unresolvedVirtualsTablesInfo.SignatureTypes = Array.Empty<IIl2CppRuntimeType[]>().AsReadOnly();
			UnresolvedVirtualsTablesInfo virtualCallTables = unresolvedVirtualsTablesInfo;
			WriteCodeRegistration(context, empty4, empty, empty2, empty3, virtualCallTables, empty5, empty6, codeGenModules, CodeRegistrationWriterMode.PerAssemblyGlobal);
		}

		public static string CodeRegistrationTableName(ReadOnlyContext context)
		{
			return context.Global.Services.Naming.ForMetadataGlobalVar("g_CodeRegistration");
		}

		private static void WriteCodeRegistration(SourceWritingContext context, TableInfo invokerTable, TableInfo reversePInvokeWrappersTable, TableInfo genericMethodPointerTable, TableInfo genericAdjustorThunkTable, UnresolvedVirtualsTablesInfo virtualCallTables, TableInfo interopDataTable, TableInfo windowsRuntimeFactoryTable, ReadOnlyCollection<string> codeGenModules, CodeRegistrationWriterMode mode)
		{
			using (ICppCodeWriter cppCodeWriter = context.CreateProfiledSourceWriterInOutputDirectory("Il2CppCodeRegistration.cpp"))
			{
				if (reversePInvokeWrappersTable.Count > 0)
				{
					cppCodeWriter.WriteLine(reversePInvokeWrappersTable.GetDeclaration());
				}
				if (genericMethodPointerTable.Count > 0)
				{
					cppCodeWriter.WriteLine(genericMethodPointerTable.GetDeclaration());
				}
				if (genericAdjustorThunkTable.Count > 0)
				{
					cppCodeWriter.WriteLine(genericAdjustorThunkTable.GetDeclaration());
				}
				if (invokerTable.Count > 0)
				{
					cppCodeWriter.WriteLine(invokerTable.GetDeclaration());
				}
				if (virtualCallTables.MethodPointersInfo.Count > 0)
				{
					cppCodeWriter.WriteLine(virtualCallTables.MethodPointersInfo.GetDeclaration());
				}
				if (interopDataTable.Count > 0)
				{
					cppCodeWriter.WriteLine(interopDataTable.GetDeclaration());
				}
				if (windowsRuntimeFactoryTable.Count > 0)
				{
					cppCodeWriter.WriteLine(windowsRuntimeFactoryTable.GetDeclaration());
				}
				if (mode == CodeRegistrationWriterMode.AllAssemblies || mode == CodeRegistrationWriterMode.PerAssemblyGlobal)
				{
					foreach (string codeGenModule in codeGenModules)
					{
						cppCodeWriter.WriteLine("IL2CPP_EXTERN_C_CONST Il2CppCodeGenModule " + codeGenModule + ";");
					}
					cppCodeWriter.WriteArrayInitializer("const Il2CppCodeGenModule*", "g_CodeGenModules", codeGenModules.Select(Emit.AddressOf), externArray: true, nullTerminate: false);
				}
				string variableName = CodeRegistrationTableName(context);
				string[] array = new string[15];
				int count = reversePInvokeWrappersTable.Count;
				array[0] = count.ToString(CultureInfo.InvariantCulture);
				array[1] = ((reversePInvokeWrappersTable.Count > 0) ? reversePInvokeWrappersTable.Name : "NULL");
				count = genericMethodPointerTable.Count;
				array[2] = count.ToString(CultureInfo.InvariantCulture);
				array[3] = ((genericMethodPointerTable.Count > 0) ? genericMethodPointerTable.Name : "NULL");
				array[4] = ((genericAdjustorThunkTable.Count > 0) ? genericAdjustorThunkTable.Name : "NULL");
				count = invokerTable.Count;
				array[5] = count.ToString(CultureInfo.InvariantCulture);
				array[6] = ((invokerTable.Count > 0) ? invokerTable.Name : "NULL");
				count = virtualCallTables.MethodPointersInfo.Count;
				array[7] = count.ToString(CultureInfo.InvariantCulture);
				array[8] = ((virtualCallTables.MethodPointersInfo.Count > 0) ? virtualCallTables.MethodPointersInfo.Name : "NULL");
				count = interopDataTable.Count;
				array[9] = count.ToString(CultureInfo.InvariantCulture);
				array[10] = ((interopDataTable.Count > 0) ? interopDataTable.Name : "NULL");
				count = windowsRuntimeFactoryTable.Count;
				array[11] = count.ToString(CultureInfo.InvariantCulture);
				array[12] = ((windowsRuntimeFactoryTable.Count > 0) ? windowsRuntimeFactoryTable.Name : "NULL");
				array[13] = codeGenModules.Count.ToString(CultureInfo.InvariantCulture);
				array[14] = ((codeGenModules.Count > 0) ? "g_CodeGenModules" : "NULL");
				cppCodeWriter.WriteStructInitializer("const Il2CppCodeRegistration", variableName, array, externStruct: true);
				if (mode == CodeRegistrationWriterMode.AllAssemblies || mode == CodeRegistrationWriterMode.PerAssemblyGlobal)
				{
					WriteGlobalCodeRegistrationCalls(context, mode, cppCodeWriter);
				}
			}
		}

		private static void WriteGlobalCodeRegistrationCalls(SourceWritingContext context, CodeRegistrationWriterMode mode, ICppCodeWriter writer)
		{
			string text = "NULL";
			if (mode == CodeRegistrationWriterMode.AllAssemblies)
			{
				writer.WriteLine("IL2CPP_EXTERN_C_CONST Il2CppMetadataRegistration g_MetadataRegistration;");
				text = "&g_MetadataRegistration";
			}
			if (context.Global.Parameters.EnableReload)
			{
				writer.WriteLine("#if IL2CPP_ENABLE_RELOAD");
				writer.WriteLine("extern \"C\" void ClearMethodMetadataInitializedFlags();");
				writer.WriteLine("#endif");
			}
			string text2 = "static";
			string type = text2 + " const Il2CppCodeGenOptions";
			string[] obj = new string[2]
			{
				context.Global.Parameters.CanShareEnumTypes ? "true" : "false",
				null
			};
			int maximumRecursiveGenericDepth = context.Global.InputData.MaximumRecursiveGenericDepth;
			obj[1] = maximumRecursiveGenericDepth.ToString();
			writer.WriteStructInitializer(type, "s_Il2CppCodeGenOptions", obj, externStruct: false);
			writer.WriteLine("void s_Il2CppCodegenRegistration()");
			writer.BeginBlock();
			writer.WriteLine("il2cpp_codegen_register (&g_CodeRegistration, " + text + ", &s_Il2CppCodeGenOptions);");
			if (context.Global.Parameters.EnableDebugger)
			{
				writer.WriteLine("#if IL2CPP_MONO_DEBUGGER");
				writer.WriteLine("il2cpp_codegen_register_debugger_data(NULL);");
				writer.WriteLine("#endif");
			}
			if (context.Global.Parameters.EnableReload)
			{
				writer.WriteLine("#if IL2CPP_ENABLE_RELOAD");
				writer.WriteLine("il2cpp_codegen_register_metadata_initialized_cleanup(ClearMethodMetadataInitializedFlags);");
				writer.WriteLine("#endif");
			}
			writer.EndBlock();
			writer.WriteLine("#if RUNTIME_IL2CPP");
			writer.WriteLine("typedef void (*CodegenRegistrationFunction)();");
			writer.WriteLine("CodegenRegistrationFunction g_CodegenRegistration = s_Il2CppCodegenRegistration;");
			writer.WriteLine("#endif");
		}

		private static TableInfo WriteReversePInvokeWrappersTable(SourceWritingContext context)
		{
			TableInfo empty = TableInfo.Empty;
			ReadOnlyCollection<KeyValuePair<MethodReference, uint>> sortedItems = context.Global.PrimaryWriteResults.ReversePInvokeWrappers.SortedItems;
			if (sortedItems.Count == 0)
			{
				return empty;
			}
			using (IGeneratedCodeWriter writer = context.CreateProfiledGeneratedCodeSourceWriterInOutputDirectory("Il2CppReversePInvokeWrapperTable.cpp"))
			{
				if (sortedItems.Count == 0)
				{
					return empty;
				}
				using (IEnumerator<KeyValuePair<MethodReference, uint>> enumerator = sortedItems.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						ReversePInvokeMethodBodyWriter.Create(managedMethod: enumerator.Current.Key, context: context).WriteMethodDeclaration(writer);
					}
				}
				return writer.WriteTable("const Il2CppMethodPointer", context.Global.Services.Naming.ForMetadataGlobalVar("g_ReversePInvokeWrapperPointers"), sortedItems, (KeyValuePair<MethodReference, uint> m) => $"reinterpret_cast<Il2CppMethodPointer>({context.Global.Services.Naming.ForReversePInvokeWrapperMethod(m.Key)})", externTable: true);
			}
		}

		private static TableInfo WriteInteropDataTable(SourceWritingContext context)
		{
			TableInfo empty = TableInfo.Empty;
			ReadOnlyCollection<KeyValuePair<IIl2CppRuntimeType, InteropData>> readOnlyCollection = InteropDataCollector.Collect(context);
			if (readOnlyCollection.Count == 0)
			{
				return empty;
			}
			IGeneratedCodeWriter writer = context.CreateProfiledGeneratedCodeSourceWriterInOutputDirectory("Il2CppInteropDataTable.cpp");
			try
			{
				if (readOnlyCollection.Count == 0)
				{
					return empty;
				}
				writer.WriteArrayInitializer("Il2CppInteropData", context.Global.Services.Naming.ForMetadataGlobalVar("g_Il2CppInteropData"), readOnlyCollection.Select(delegate(KeyValuePair<IIl2CppRuntimeType, InteropData> pair)
				{
					Extensions.Deconstruct(pair, out var key, out var value);
					IIl2CppRuntimeType il2CppRuntimeType = key;
					InteropData interopData = value;
					TypeReference type = il2CppRuntimeType.Type;
					string text = "NULL";
					string text2 = "NULL";
					string text3 = "NULL";
					string text4 = "NULL";
					string text5 = "NULL";
					string text6 = "NULL";
					if (interopData.HasDelegatePInvokeWrapperMethod)
					{
						text = context.Global.Services.Naming.ForDelegatePInvokeWrapper(type);
						writer.WriteLine("IL2CPP_EXTERN_C void " + text + "();");
					}
					if (interopData.HasPInvokeMarshalingFunctions)
					{
						DefaultMarshalInfoWriter defaultMarshalInfoWriter = MarshalDataCollector.MarshalInfoWriterFor(context, type, MarshalType.PInvoke);
						text2 = defaultMarshalInfoWriter.MarshalToNativeFunctionName;
						text3 = defaultMarshalInfoWriter.MarshalFromNativeFunctionName;
						text4 = defaultMarshalInfoWriter.MarshalCleanupFunctionName;
						writer.WriteLine("IL2CPP_EXTERN_C void " + text2 + "(void* managedStructure, void* marshaledStructure);");
						writer.WriteLine("IL2CPP_EXTERN_C void " + text3 + "(void* marshaledStructure, void* managedStructure);");
						writer.WriteLine("IL2CPP_EXTERN_C void " + text4 + "(void* marshaledStructure);");
					}
					if (interopData.HasCreateCCWFunction)
					{
						text5 = context.Global.Services.Naming.ForCreateComCallableWrapperFunction(type);
						writer.WriteLine("IL2CPP_EXTERN_C Il2CppIUnknown* " + text5 + "(RuntimeObject* obj);");
					}
					if (interopData.HasGuid)
					{
						if (type.Resolve().HasCLSID())
						{
							writer.WriteStatement(Emit.Assign("const Il2CppGuid " + context.Global.Services.Naming.ForTypeNameOnly(type) + "::CLSID", type.Resolve().GetGuid(context).ToInitializer()));
							text6 = "&" + context.Global.Services.Naming.ForTypeNameOnly(type) + "::CLSID";
						}
						else if (type.HasIID(context))
						{
							writer.WriteStatement(Emit.Assign("const Il2CppGuid " + context.Global.Services.Naming.ForTypeNameOnly(type) + "::IID", type.GetGuid(context).ToInitializer()));
							text6 = "&" + context.Global.Services.Naming.ForTypeNameOnly(type) + "::IID";
						}
						else
						{
							TypeReference typeReference = context.Global.Services.WindowsRuntime.ProjectToWindowsRuntime(type);
							if (typeReference.IsWindowsRuntimeDelegate(context))
							{
								text6 = "&" + context.Global.Services.Naming.ForWindowsRuntimeDelegateComCallableWrapperInterface(type) + "::IID";
							}
							else
							{
								if (typeReference == type)
								{
									throw new InvalidOperationException("InteropData says type ('" + type.FullName + "') has a GUID, but no GUID could be found for it.");
								}
								writer.AddIncludeForTypeDefinition(typeReference);
								text6 = "&" + context.Global.Services.Naming.ForTypeNameOnly(typeReference) + "::IID";
							}
						}
						writer.AddIncludeForTypeDefinition(type);
					}
					writer.WriteLine("IL2CPP_EXTERN_C_CONST RuntimeType " + context.Global.Services.Naming.ForIl2CppType(il2CppRuntimeType) + ";");
					string text7 = SerializeIl2CppType(context, il2CppRuntimeType);
					return "{ " + text + ", " + text2 + ", " + text3 + ", " + text4 + ", " + text5 + ", " + text6 + ", " + text7 + " } /* " + type.FullName + " */";
				}), externArray: true);
				return new TableInfo(readOnlyCollection.Count, "Il2CppInteropData", context.Global.Services.Naming.ForMetadataGlobalVar("g_Il2CppInteropData"), externTable: true);
			}
			finally
			{
				if (writer != null)
				{
					writer.Dispose();
				}
			}
		}

		private static TableInfo WriteWindowsRuntimeFactoryTable(SourceWritingContext context)
		{
			TableInfo empty = TableInfo.Empty;
			if (context.Global.Parameters.UsingTinyClassLibraries)
			{
				return empty;
			}
			List<WindowsRuntimeFactoryData> list = DictionaryExtensions.ItemsSortedByKey(context.Global.PrimaryCollectionResults.WindowsRuntimeData).SelectMany((KeyValuePair<AssemblyDefinition, CollectedWindowsRuntimeData> pair) => pair.Value.RuntimeFactories).ToList();
			if (list.Count == 0)
			{
				return empty;
			}
			using (IGeneratedCodeWriter generatedCodeWriter = context.CreateProfiledGeneratedCodeSourceWriterInOutputDirectory("Il2CppWindowsRuntimeFactoriesTable.cpp"))
			{
				INamingService naming = context.Global.Services.Naming;
				foreach (WindowsRuntimeFactoryData item in list)
				{
					generatedCodeWriter.WriteExternForIl2CppType(item.RuntimeType);
					generatedCodeWriter.WriteStatement("Il2CppIActivationFactory* " + naming.ForCreateWindowsRuntimeFactoryFunction(item.TypeDefinition) + "()");
				}
				empty = new TableInfo(list.Count, "Il2CppWindowsRuntimeFactoryTableEntry", context.Global.Services.Naming.ForMetadataGlobalVar("g_WindowsRuntimeFactories"), externTable: true);
				generatedCodeWriter.WriteLine(empty.GetDeclaration());
				generatedCodeWriter.WriteLine($"Il2CppWindowsRuntimeFactoryTableEntry g_WindowsRuntimeFactories[{list.Count}] =");
				using (new BlockWriter(generatedCodeWriter, semicolon: true))
				{
					foreach (WindowsRuntimeFactoryData item2 in list)
					{
						generatedCodeWriter.WriteLine("{ &" + naming.ForIl2CppType(item2.RuntimeType) + ", reinterpret_cast<Il2CppMethodPointer>(" + naming.ForCreateWindowsRuntimeFactoryFunction(item2.TypeDefinition) + ") },");
					}
					return empty;
				}
			}
		}

		private static TableInfo WriteGenericMethodPointerTable(SourceWritingContext context, ReadOnlyMethodTables methodPointerTables)
		{
			using (IGeneratedCodeWriter writer = context.CreateProfiledGeneratedCodeSourceWriterInOutputDirectory("Il2CppGenericMethodPointerTable.cpp"))
			{
				return new MethodTableWriter(writer).Write(context, methodPointerTables);
			}
		}

		private static TableInfo WriteGenericAdjustorThunkTable(SourceWritingContext context, ReadOnlyMethodTables methodPointerTables)
		{
			using (IGeneratedCodeWriter writer = context.CreateProfiledGeneratedCodeSourceWriterInOutputDirectory("Il2CppGenericAdjustorThunkTable.cpp"))
			{
				return new GenericAdjustorThunkTableWriter(writer).Write(context);
			}
		}

		private static TableInfo WriteInvokerTable(SourceWritingContext context, ReadOnlyInvokerCollection invokerCollection)
		{
			using (IGeneratedCodeWriter writer = context.CreateProfiledGeneratedCodeSourceWriterInOutputDirectory("Il2CppInvokerTable.cpp"))
			{
				return InvokerWriter.Write(context.Global.AsReadOnly(), writer, invokerCollection);
			}
		}

		private static string SerializeIl2CppType(SourceWritingContext context, IIl2CppRuntimeType type)
		{
			return "&" + context.Global.Services.Naming.ForIl2CppType(type);
		}
	}
}
