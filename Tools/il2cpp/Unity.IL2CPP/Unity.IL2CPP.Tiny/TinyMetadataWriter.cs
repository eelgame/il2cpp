using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using Unity.Cecil.Awesome;
using Unity.Cecil.Awesome.Ordering;
using Unity.IL2CPP.AssemblyConversion;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Results;
using Unity.IL2CPP.Contexts.Results.Phases;
using Unity.IL2CPP.Metadata;
using Unity.IL2CPP.Naming;
using Unity.IL2CPP.StringLiterals;

namespace Unity.IL2CPP.Tiny
{
	public class TinyMetadataWriter : IMetadataWriterImplementation
	{
		public const int NumberOfPackedInterfaceOffsetsPerElement32 = 2;

		public const int NumberOfPackedInterfaceOffsetsPerElement64 = 4;

		private readonly TinyWriteContext _context;

		private readonly ReadOnlyCollection<AssemblyDefinition> _assemblies;

		public TinyMetadataWriter(TinyWriteContext context, ReadOnlyCollection<AssemblyDefinition> assemblies)
		{
			_context = context;
			_assemblies = assemblies;
		}

		public void Write(out IStringLiteralCollection stringLiteralCollection, out IFieldReferenceCollection fieldReferenceCollection)
		{
			stringLiteralCollection = null;
			fieldReferenceCollection = null;
			WriteTinyMetadata(_context, _assemblies);
		}

		public static void WriteTinyMetadata(TinyWriteContext context, ReadOnlyCollection<AssemblyDefinition> assemblies)
		{
			ReadOnlyCollection<GenericInstanceType> types = context.Global.Results.PrimaryCollection.Generics.Types;
			IMethodCollectorResults methods = context.Global.Results.PrimaryWrite.Methods;
			IGenericMethodCollectorResults genericMethods = context.Global.Results.PrimaryWrite.GenericMethods;
			ReadOnlyCollection<TinyPrimaryWriteResult> writeResults = (from pair in DictionaryExtensions.ItemsSortedByKey(context.Global.Results.PrimaryWrite.TinyAssemblyResults)
				select pair.Value).Append(context.Global.Results.PrimaryWrite.TinyGenericResults).ToList().AsReadOnly();
			WriteTinyTypeTable(context);
			WriteTinyMethodTable(context, methods, genericMethods);
			WriteStringLiteralTable(context);
			WriteStaticFieldInitializer(context, assemblies, types);
			WriteStaticConstructorInvoker(context.Global.AsBig().CreateSourceWritingContext(), writeResults);
			WriteModuleInitializerInvoker(context, writeResults);
		}

		private static void WriteTinyMethodTable(TinyWriteContext context, IMethodCollectorResults methodCollector, IGenericMethodCollectorResults genericMethodCollector)
		{
			using (ICppCodeWriter cppCodeWriter = context.CreateProfiledSourceWriterInOutputDirectory("TinyMethods.cpp"))
			{
				StringBuilder stringBuilder = new StringBuilder();
				StringBuilder stringBuilder2 = new StringBuilder();
				StringBuilder stringBuilder3 = new StringBuilder();
				StringBuilder stringBuilder4 = new StringBuilder();
				List<MethodReference> list = new List<MethodReference>();
				list.AddRange(genericMethodCollector.SortedKeys.Select((Il2CppMethodSpec g) => g.GenericMethod));
				list.AddRange(methodCollector.SortedKeys);
				int num = 0;
				foreach (MethodReference item in list)
				{
					string text = ((num != 0) ? "\t" : string.Empty);
					stringBuilder.AppendLine("extern \"C\" void " + context.Global.Services.Naming.ForMethodNameOnly(item) + " ();");
					stringBuilder2.AppendLine(text + "(Il2CppMethodPointer)&" + context.Global.Services.Naming.ForMethodNameOnly(item) + ",");
					stringBuilder3.AppendLine("TinyMethod " + context.Global.Services.Naming.ForRuntimeMethodInfo(item) + " = { \"" + RemoveReturnTypeFromMethodName(item.FullName).Replace("::", ".") + "\" };");
					stringBuilder4.AppendLine(text + "&" + context.Global.Services.Naming.ForRuntimeMethodInfo(item) + ",");
					num++;
				}
				cppCodeWriter.WriteLine("#if defined(IL2CPP_TINY_DEBUG_METADATA)");
				cppCodeWriter.WriteLine("#include \"il2cpp-debug-metadata.h\"");
				cppCodeWriter.Write(stringBuilder.ToString());
				cppCodeWriter.WriteLine($"int g_NumberOfIl2CppTinyMethods = {num};");
				cppCodeWriter.WriteLine("Il2CppMethodPointer g_Il2CppTinyMethodPointers[] =");
				using (new BlockWriter(cppCodeWriter, semicolon: true))
				{
					cppCodeWriter.Write(stringBuilder2.ToString());
				}
				cppCodeWriter.WriteLine(stringBuilder3.ToString());
				cppCodeWriter.WriteLine("TinyMethod* g_Il2CppTinyMethods[] =");
				using (new BlockWriter(cppCodeWriter, semicolon: true))
				{
					cppCodeWriter.Write(stringBuilder4.ToString());
				}
				cppCodeWriter.WriteLine("#endif");
			}
		}

		private static string RemoveReturnTypeFromMethodName(string name)
		{
			return name.Substring(name.IndexOf(' ') + 1);
		}

		private static void WriteTinyTypeTable(TinyWriteContext context)
		{
			using (ICppCodeWriter cppCodeWriter = context.CreateProfiledSourceWriterInOutputDirectory("TinyTypes.cpp"))
			{
				cppCodeWriter.AddInclude("il2cpp-object-internals.h");
				cppCodeWriter.WriteLine("TinyType* g_SystemTypeTinyType = NULL;");
				cppCodeWriter.WriteLine("TinyType* g_ArrayTinyType = NULL;");
				cppCodeWriter.WriteLine();
				context.Global.AsBig().CreateMinimalContext();
				ITinyTypeMetadataResults tinyTypeMetadata = context.Global.Results.SecondaryCollection.TinyTypeMetadata;
				ReadOnlyCollection<TinyTypeEntry> allEntries = tinyTypeMetadata.GetAllEntries();
				Dictionary<TinyVirtualMethodData, int> dictionary = CollectUniqueVirtualMethods(allEntries);
				if (allEntries.Count > 0)
				{
					cppCodeWriter.WriteLine("#if IL2CPP_SIZEOF_VOID_P == 4");
					foreach (TinyTypeEntry item in allEntries)
					{
						cppCodeWriter.WriteLine($"IL2CPP_EXTERN_C const uint32_t {item.OffsetConstantName} = {item.Offset32};");
					}
					cppCodeWriter.WriteLine("#else");
					foreach (TinyTypeEntry item2 in allEntries)
					{
						cppCodeWriter.WriteLine($"IL2CPP_EXTERN_C const uint32_t {item2.OffsetConstantName} = {item2.Offset64};");
					}
					cppCodeWriter.WriteLine("#endif");
					int num = 0;
					cppCodeWriter.WriteLine("uintptr_t g_Il2CppTinyTypeUniverse[] =");
					using (new BlockWriter(cppCodeWriter, semicolon: true))
					{
						foreach (TinyTypeEntry item3 in allEntries)
						{
							cppCodeWriter.Write("0, ");
							cppCodeWriter.Write($"{item3.PackedCounts}, ");
							MethodReference[] virtualMethods = item3.VirtualMethods;
							foreach (MethodReference methodReference in virtualMethods)
							{
								int num2 = 0;
								if (methodReference != null && !methodReference.HasGenericParameters)
								{
									TinyVirtualMethodData key = new TinyVirtualMethodData
									{
										VirtualMethod = methodReference,
										DerivedDeclaringType = item3.Type
									};
									num2 = dictionary[key];
								}
								cppCodeWriter.Write($"{num2}, ");
							}
							foreach (TypeReference item4 in item3.TypeHierarchy)
							{
								TinyTypeEntry typeEntry = tinyTypeMetadata.GetTypeEntry(item4);
								cppCodeWriter.Write($"IL2CPP_SIZEOF_VOID_P == 4 ? {typeEntry.Offset32} : {typeEntry.Offset64}, ");
							}
							foreach (TypeReference @interface in item3.Interfaces)
							{
								TinyTypeEntry typeEntry2 = tinyTypeMetadata.GetTypeEntry(@interface);
								cppCodeWriter.Write($"IL2CPP_SIZEOF_VOID_P == 4 ? {typeEntry2.Offset32} : {typeEntry2.Offset64}, ");
							}
							if (item3.InterfaceOffsets.Any())
							{
								EmitPackedInterfaceOffsetsFor(item3, cppCodeWriter);
							}
							if (context.Global.Parameters.EnableTinyDebugging)
							{
								cppCodeWriter.Write($"{num}, ");
							}
							if (context.Global.Parameters.EmitComments)
							{
								cppCodeWriter.Write($"// {item3.Type.AssemblyQualifiedName()} - id={num}");
							}
							cppCodeWriter.WriteLine();
							num++;
						}
					}
				}
				cppCodeWriter.WriteLine();
				Dictionary<TinyVirtualMethodData, int>.KeyCollection keys = dictionary.Keys;
				foreach (TinyVirtualMethodData item5 in keys)
				{
					cppCodeWriter.WriteLine("IL2CPP_EXTERN_C void " + MethodNameFor(context, item5) + " ();");
				}
				cppCodeWriter.WriteLine();
				cppCodeWriter.WriteLine("Il2CppMethodPointer g_AllVirtualMethods[] =");
				using (new BlockWriter(cppCodeWriter, semicolon: true))
				{
					if (dictionary.Any())
					{
						foreach (TinyVirtualMethodData item6 in keys)
						{
							if (item6.VirtualMethod.Resolve().IsAbstract)
							{
								cppCodeWriter.Write("0,");
								if (context.Global.Parameters.EmitComments)
								{
									cppCodeWriter.Write("// abstract " + item6.VirtualMethod.AssemblyQualifiedName());
								}
								cppCodeWriter.WriteLine();
							}
							else
							{
								cppCodeWriter.Write(MethodNameFor(context, item6) + ",");
								if (context.Global.Parameters.EmitComments)
								{
									cppCodeWriter.Write("// " + item6.VirtualMethod.AssemblyQualifiedName());
								}
								cppCodeWriter.WriteLine();
							}
						}
					}
					else
					{
						cppCodeWriter.WriteLine("NULL");
					}
				}
				cppCodeWriter.WriteLine();
				cppCodeWriter.WriteLine("const Il2CppMethodPointer* Il2CppGetTinyVirtualMethodUniverse()");
				using (new BlockWriter(cppCodeWriter))
				{
					cppCodeWriter.WriteLine("return g_AllVirtualMethods;");
				}
				cppCodeWriter.WriteLine();
				cppCodeWriter.WriteLine("uint32_t Il2CppGetTinyTypeUniverseTypeCount()");
				using (new BlockWriter(cppCodeWriter))
				{
					cppCodeWriter.WriteLine($"return {allEntries.Count};");
				}
				cppCodeWriter.WriteLine();
				cppCodeWriter.WriteLine("uint8_t* Il2CppGetTinyTypeUniverse()");
				using (new BlockWriter(cppCodeWriter))
				{
					if (allEntries.Count > 0)
					{
						cppCodeWriter.WriteLine("return reinterpret_cast<uint8_t*>(g_Il2CppTinyTypeUniverse);");
					}
					else
					{
						cppCodeWriter.WriteLine("return NULL;");
					}
				}
				cppCodeWriter.WriteLine("void InitializeTypeInstances()");
				using (new BlockWriter(cppCodeWriter))
				{
					if (context.Global.Services.TypeProvider.SystemType != null)
					{
						TinyTypeEntry typeEntry3 = context.Global.Results.SecondaryCollection.TinyTypeMetadata.GetTypeEntry(context.Global.Services.TypeProvider.SystemType);
						cppCodeWriter.WriteLine($"g_SystemTypeTinyType = reinterpret_cast<TinyType*>(Il2CppGetTinyTypeUniverse() + (IL2CPP_SIZEOF_VOID_P == 4 ? {typeEntry3.Offset32} : {typeEntry3.Offset64}));");
						if (context.Global.Services.TypeProvider.SystemArray != null)
						{
							TinyTypeEntry typeEntry4 = context.Global.Results.SecondaryCollection.TinyTypeMetadata.GetTypeEntry(context.Global.Services.TypeProvider.SystemArray);
							cppCodeWriter.WriteLine($"g_ArrayTinyType = reinterpret_cast<TinyType*>(Il2CppGetTinyTypeUniverse() + (IL2CPP_SIZEOF_VOID_P == 4 ? {typeEntry4.Offset32} : {typeEntry4.Offset64}));");
						}
						else
						{
							cppCodeWriter.WriteLine("g_ArrayTinyType = NULL;");
						}
					}
				}
			}
		}

		public static void WriteStringLiteralTable(TinyWriteContext context)
		{
			ITinyStringMetadataResults tinyStringMetadata = context.Global.Results.SecondaryCollection.TinyStringMetadata;
			ReadOnlyCollection<StringLiteralEntry> entries = tinyStringMetadata.GetEntries();
			int stringLiteralCount = tinyStringMetadata.GetStringLiteralCount();
			using (ICppCodeWriter cppCodeWriter = context.CreateProfiledSourceWriterInOutputDirectory("StringLiteralsOffsets.h"))
			{
				if (stringLiteralCount > 0)
				{
					cppCodeWriter.WriteLine("#if IL2CPP_SIZEOF_VOID_P == 4");
					foreach (StringLiteralEntry item in entries)
					{
						cppCodeWriter.WriteLine($"const uint32_t {item.OffsetConstantName} = {item.Offset32};");
					}
					cppCodeWriter.WriteLine("#elif IL2CPP_SIZEOF_VOID_P == 8");
					foreach (StringLiteralEntry item2 in entries)
					{
						cppCodeWriter.WriteLine($"const uint32_t {item2.OffsetConstantName} = {item2.Offset64};");
					}
					cppCodeWriter.WriteLine("#else");
					cppCodeWriter.WriteLine("#error Unknown architecture!");
					cppCodeWriter.WriteLine("#endif");
				}
			}
			using (ICppCodeWriter cppCodeWriter2 = context.CreateProfiledSourceWriterInOutputDirectory("StringLiterals.cpp"))
			{
				if (stringLiteralCount > 0)
				{
					cppCodeWriter2.WriteLine("#if IL2CPP_SIZEOF_VOID_P == 4");
					cppCodeWriter2.WriteLine("uintptr_t g_Il2CppStringLiterals[] =");
					using (new BlockWriter(cppCodeWriter2, semicolon: true))
					{
						foreach (string item3 in tinyStringMetadata.GetStringLines32())
						{
							cppCodeWriter2.WriteLine(item3);
						}
					}
					cppCodeWriter2.WriteLine("#elif IL2CPP_SIZEOF_VOID_P == 8");
					cppCodeWriter2.WriteLine("uintptr_t g_Il2CppStringLiterals[] =");
					using (new BlockWriter(cppCodeWriter2, semicolon: true))
					{
						foreach (string item4 in tinyStringMetadata.GetStringLines64())
						{
							cppCodeWriter2.WriteLine(item4);
						}
					}
					cppCodeWriter2.WriteLine("#else");
					cppCodeWriter2.WriteLine("#error Unknown architecture!");
					cppCodeWriter2.WriteLine("#endif");
				}
				cppCodeWriter2.WriteLine();
				cppCodeWriter2.WriteLine("uint32_t Il2CppGetStringLiteralCount()");
				using (new BlockWriter(cppCodeWriter2, semicolon: true))
				{
					cppCodeWriter2.WriteLine($"return {stringLiteralCount};");
				}
				cppCodeWriter2.WriteLine();
				cppCodeWriter2.WriteLine("uint8_t* Il2CppGetStringLiterals()");
				using (new BlockWriter(cppCodeWriter2, semicolon: true))
				{
					if (stringLiteralCount > 0)
					{
						cppCodeWriter2.WriteLine("return reinterpret_cast<uint8_t*>(g_Il2CppStringLiterals);");
					}
					else
					{
						cppCodeWriter2.WriteLine("return NULL;");
					}
				}
				cppCodeWriter2.WriteLine();
				cppCodeWriter2.WriteLine("extern uint8_t* Il2CppGetTinyTypeUniverse();");
				cppCodeWriter2.WriteLine("#include \"il2cpp-object-internals.h\"");
				cppCodeWriter2.WriteLine();
				cppCodeWriter2.WriteLine("TinyType* g_StringTinyType;");
				cppCodeWriter2.WriteLine("void InitializeStringLiterals()");
				using (new BlockWriter(cppCodeWriter2, semicolon: true))
				{
					if (stringLiteralCount > 0)
					{
						TinyTypeEntry typeEntry = context.Global.Results.SecondaryCollection.TinyTypeMetadata.GetTypeEntry(context.Global.Services.TypeProvider.StringTypeReference);
						cppCodeWriter2.WriteLine($"g_StringTinyType = reinterpret_cast<TinyType*>(Il2CppGetTinyTypeUniverse() + (IL2CPP_SIZEOF_VOID_P == 4 ? {typeEntry.Offset32} : {typeEntry.Offset64}));");
						cppCodeWriter2.WriteLine("Il2CppString* literals = reinterpret_cast<Il2CppString*>(g_Il2CppStringLiterals);");
						cppCodeWriter2.WriteLine("Il2CppString* literalsEnd = reinterpret_cast<Il2CppString*>(reinterpret_cast<uintptr_t>(literals) + IL2CPP_ARRAY_SIZE(g_Il2CppStringLiterals) * sizeof(uintptr_t));");
						cppCodeWriter2.WriteLine("while (literals != literalsEnd)");
						using (new BlockWriter(cppCodeWriter2))
						{
							cppCodeWriter2.WriteLine("literals->object.klass = g_StringTinyType;");
							cppCodeWriter2.WriteLine("int rawOffset = sizeof(TinyType*) + sizeof(int32_t) + sizeof(Il2CppChar) * (literals->length + 1);");
							cppCodeWriter2.WriteLine("int alignedOffset = (rawOffset + IL2CPP_SIZEOF_VOID_P - 1) & ~(IL2CPP_SIZEOF_VOID_P - 1);");
							cppCodeWriter2.WriteLine("literals = reinterpret_cast<Il2CppString*>(reinterpret_cast<uint8_t*>(literals) + alignedOffset);");
							return;
						}
					}
				}
			}
		}

		private static void WriteStaticFieldInitializer(TinyWriteContext context, IEnumerable<AssemblyDefinition> allAssemblies, ReadOnlyCollection<GenericInstanceType> genericTypes)
		{
			using (ICppCodeWriter cppCodeWriter = context.CreateProfiledSourceWriterInOutputDirectory("StaticInitialization.cpp"))
			{
				List<TypeReference> list = new List<TypeReference>();
				List<TypeReference> list2 = new List<TypeReference>();
				foreach (AssemblyDefinition allAssembly in allAssemblies)
				{
					foreach (TypeDefinition allType in allAssembly.MainModule.GetAllTypes())
					{
						if (!allType.HasGenericParameters)
						{
							TypeDefinition typeDef = allType.Resolve();
							if (RequiresStaticFieldStorage(typeDef))
							{
								list.Add(allType);
							}
							if (RequiresStaticFieldRVAStorage(typeDef))
							{
								list2.Add(allType);
							}
						}
					}
				}
				foreach (GenericInstanceType genericType in genericTypes)
				{
					TypeDefinition typeDef2 = genericType.Resolve();
					if (RequiresStaticFieldStorage(typeDef2))
					{
						list.Add(genericType);
					}
					if (RequiresStaticFieldRVAStorage(typeDef2))
					{
						list2.Add(genericType);
					}
				}
				foreach (TypeReference item in list)
				{
					cppCodeWriter.WriteLine("extern void* {0};", context.Global.Services.Naming.ForStaticFieldsStructStorage(item));
				}
				cppCodeWriter.WriteLine();
				cppCodeWriter.WriteLine("const void* StaticFieldsStorage[] = ");
				using (new BlockWriter(cppCodeWriter, semicolon: true))
				{
					foreach (TypeReference item2 in list)
					{
						cppCodeWriter.WriteLine("&" + context.Global.Services.Naming.ForStaticFieldsStructStorage(item2) + ",");
					}
					cppCodeWriter.WriteLine("NULL");
				}
				cppCodeWriter.WriteLine();
				cppCodeWriter.WriteLine("const void** GetStaticFieldsStorageArray()");
				using (new BlockWriter(cppCodeWriter))
				{
					cppCodeWriter.WriteLine("return StaticFieldsStorage;");
				}
				cppCodeWriter.WriteLine();
				foreach (TypeReference item3 in list2)
				{
					EmitFieldRVAInitializersForType(context, item3, cppCodeWriter);
				}
			}
		}

		private static void WriteStaticConstructorInvoker(SourceWritingContext context, ReadOnlyCollection<TinyPrimaryWriteResult> writeResults)
		{
			using (ICppCodeWriter cppCodeWriter = context.CreateProfiledSourceWriterInOutputDirectory("StaticConstructors.cpp"))
			{
				foreach (TinyPrimaryWriteResult writeResult in writeResults)
				{
					if (!string.IsNullOrEmpty(writeResult.StaticConstructorMethodName))
					{
						cppCodeWriter.WriteLine("IL2CPP_EXTERN_C void " + writeResult.StaticConstructorMethodName + "();");
					}
				}
				cppCodeWriter.WriteLine("void Il2CppCallStaticConstructors()");
				using (new BlockWriter(cppCodeWriter))
				{
					foreach (TinyPrimaryWriteResult writeResult2 in writeResults)
					{
						if (!string.IsNullOrEmpty(writeResult2.StaticConstructorMethodName))
						{
							cppCodeWriter.WriteLine(writeResult2.StaticConstructorMethodName + "();");
						}
					}
				}
			}
		}

		private static void EmitFieldRVAInitializersForType(TinyWriteContext context, TypeReference type, ICppCodeWriter writer)
		{
			foreach (FieldDefinition field in type.Resolve().Fields)
			{
				if (!field.Attributes.HasFlag(FieldAttributes.HasFieldRVA))
				{
					continue;
				}
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.Append($"extern const uint8_t {context.Global.Services.Naming.ForStaticFieldsRVAStructStorage(field)}[{field.InitialValue.Length}] = {{");
				for (int i = 0; i < field.InitialValue.Length; i++)
				{
					stringBuilder.AppendFormat("0x{0:X}", field.InitialValue[i]);
					if (i != field.InitialValue.Length - 1)
					{
						stringBuilder.Append(", ");
					}
				}
				stringBuilder.Append("};");
				writer.WriteLine(stringBuilder.ToString());
			}
		}

		private static bool RequiresStaticFieldStorage(TypeDefinition typeDef)
		{
			if (!typeDef.Fields.Any((FieldDefinition f) => f.IsNormalStatic()))
			{
				return typeDef.StoresNonFieldsInStaticFields();
			}
			return true;
		}

		private static bool RequiresStaticFieldRVAStorage(TypeDefinition typeDef)
		{
			return typeDef.Fields.Any((FieldDefinition f) => f.Attributes.HasFlag(FieldAttributes.HasFieldRVA));
		}

		public static void WriteModuleInitializerInvoker(TinyWriteContext context, ReadOnlyCollection<TinyPrimaryWriteResult> writeResults)
		{
			using (ICppCodeWriter cppCodeWriter = context.CreateProfiledSourceWriterInOutputDirectory("ModuleInitializers.cpp"))
			{
				foreach (TinyPrimaryWriteResult writeResult in writeResults)
				{
					if (!string.IsNullOrEmpty(writeResult.ModuleInitializerMethodName))
					{
						cppCodeWriter.WriteLine("IL2CPP_EXTERN_C void " + writeResult.ModuleInitializerMethodName + "();");
					}
				}
				cppCodeWriter.WriteLine("void Il2CppCallModuleInitializers()");
				using (new BlockWriter(cppCodeWriter))
				{
					foreach (TinyPrimaryWriteResult writeResult2 in writeResults)
					{
						if (!string.IsNullOrEmpty(writeResult2.ModuleInitializerMethodName))
						{
							cppCodeWriter.WriteLine(writeResult2.ModuleInitializerMethodName + "();");
						}
					}
				}
			}
		}

		private static Dictionary<TinyVirtualMethodData, int> CollectUniqueVirtualMethods(IEnumerable<TinyTypeEntry> types)
		{
			Dictionary<TinyVirtualMethodData, int> dictionary = new Dictionary<TinyVirtualMethodData, int>();
			int num = 0;
			foreach (TinyTypeEntry type in types)
			{
				MethodReference[] virtualMethods = type.VirtualMethods;
				foreach (MethodReference methodReference in virtualMethods)
				{
					if (methodReference != null && !methodReference.HasGenericParameters)
					{
						TinyVirtualMethodData key = new TinyVirtualMethodData
						{
							VirtualMethod = methodReference,
							DerivedDeclaringType = type.Type
						};
						if (!dictionary.ContainsKey(key))
						{
							dictionary.Add(key, num);
							num++;
						}
					}
				}
			}
			return dictionary;
		}

		private static void EmitPackedInterfaceOffsetsFor(TinyTypeEntry entry, ICodeWriter writer)
		{
			writer.WriteLine();
			writer.WriteLine("#if IL2CPP_SIZEOF_VOID_P == 4");
			writer.WriteLine(EmitPackedInterfaceOffsetsLine32(entry));
			writer.WriteLine("#else");
			writer.WriteLine(EmitPackedInterfaceOffsetsLine64(entry));
			if (writer.Context.Global.Parameters.EnableTinyDebugging)
			{
				writer.WriteLine("#endif ");
			}
			else
			{
				writer.Write("#endif ");
			}
		}

		private static string EmitPackedInterfaceOffsetsLine64(TinyTypeEntry entry)
		{
			List<int> list = entry.InterfaceOffsets.ToList();
			int num = TinyTypeMetadataCollector.NumberOfPackedInterfaceOffsetElements(list.Count, 4);
			int num2 = 4 - list.Count % 4;
			for (int i = 0; i < num2; i++)
			{
				list.Add(-1);
			}
			ulong[] array = new ulong[num];
			int num3 = 0;
			for (int j = 0; j < array.Length; j++)
			{
				array[j] = (ulong)(list[num3] + ((long)list[num3 + 1] << 16) + ((long)list[num3 + 2] << 32) + ((long)list[num3 + 3] << 48));
				num3 += 4;
			}
			string text = string.Empty;
			for (int k = 0; k < num; k++)
			{
				text += $"{array[k]}U, ";
			}
			return text;
		}

		private static string EmitPackedInterfaceOffsetsLine32(TinyTypeEntry entry)
		{
			List<int> list = entry.InterfaceOffsets.ToList();
			int num = TinyTypeMetadataCollector.NumberOfPackedInterfaceOffsetElements(list.Count, 2);
			int num2 = 2 - list.Count % 2;
			for (int i = 0; i < num2; i++)
			{
				list.Add(-1);
			}
			uint[] array = new uint[num];
			int num3 = 0;
			for (int j = 0; j < array.Length; j++)
			{
				array[j] = (uint)(list[num3] + (list[num3 + 1] << 16));
				num3 += 2;
			}
			string text = string.Empty;
			for (int k = 0; k < num; k++)
			{
				text += $"{array[k]}, ";
			}
			return text;
		}

		private static string MethodNameFor(ReadOnlyContext context, TinyVirtualMethodData virtualMethodData)
		{
			if (MethodWriter.HasAdjustorThunk(virtualMethodData.VirtualMethod))
			{
				return context.Global.Services.Naming.ForMethodAdjustorThunk(virtualMethodData.VirtualMethod);
			}
			if (TinyVirtualRemap.ShouldRemap(virtualMethodData))
			{
				return TinyVirtualRemap.RemappedMethodNameFor(virtualMethodData, context.Global.Parameters.ReturnAsByRefParameter);
			}
			return context.Global.Services.Naming.ForMethodNameOnly(virtualMethodData.VirtualMethod);
		}
	}
}
