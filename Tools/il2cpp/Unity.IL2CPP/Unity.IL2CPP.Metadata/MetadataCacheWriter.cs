using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using Mono.Cecil;
using NiceIO;
using Unity.Cecil.Awesome;
using Unity.IL2CPP.AssemblyConversion.SecondaryCollection.Steps.Generics;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Collections;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Results;
using Unity.IL2CPP.Contexts.Scheduling;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.Marshaling;
using Unity.IL2CPP.Metadata.RuntimeTypes;
using Unity.IL2CPP.Naming;
using Unity.IL2CPP.StringLiterals;
using Unity.MiniProfiling;

namespace Unity.IL2CPP.Metadata
{
	public sealed class MetadataCacheWriter
	{
		private abstract class TableWriter
		{
			public int Index { get; }

			protected TableWriter(int index)
			{
				Index = index;
			}

			public abstract TableInfo Write(SourceWritingContext context);
		}

		private class TableWriterComparer : IComparer<ResultData<TableWriter, TableInfo>>
		{
			public int Compare(ResultData<TableWriter, TableInfo> x, ResultData<TableWriter, TableInfo> y)
			{
				return x.Item.Index.CompareTo(y.Item.Index);
			}
		}

		private class WriteIl2CppGenericClassTable : TableWriter
		{
			public WriteIl2CppGenericClassTable(int index)
				: base(index)
			{
			}

			public override TableInfo Write(SourceWritingContext context)
			{
				using (IGeneratedCodeWriter generatedCodeWriter = context.CreateProfiledGeneratedCodeSourceWriterInOutputDirectory(context.Global.Services.PathFactory.GetFileName("Il2CppGenericClassTable.c")))
				{
					generatedCodeWriter.AddCodeGenMetadataIncludes();
					List<IIl2CppRuntimeType> list = context.Global.Results.SecondaryCollection.Types.SortedItems.Where((IIl2CppRuntimeType t) => t.Type.IsGenericInstance).ToList();
					foreach (IIl2CppRuntimeType item in list)
					{
						generatedCodeWriter.WriteExternForGenericClass(item.Type);
					}
					return generatedCodeWriter.WriteTable("Il2CppGenericClass* const", context.Global.Services.Naming.ForMetadataGlobalVar("g_Il2CppGenericTypes"), list, (IIl2CppRuntimeType t) => "&" + context.Global.Services.Naming.ForGenericClass(t.Type), externTable: true);
				}
			}
		}

		private class WriteIl2CppGenericInstDefinitions : TableWriter
		{
			private readonly GenericInstancesCollection _genericInstCollection;

			public WriteIl2CppGenericInstDefinitions(int index, GenericInstancesCollection genericInstCollection)
				: base(index)
			{
				_genericInstCollection = genericInstCollection;
			}

			public override TableInfo Write(SourceWritingContext context)
			{
				using (IGeneratedCodeWriter writer = context.CreateProfiledGeneratedCodeSourceWriterInOutputDirectory(context.Global.Services.PathFactory.GetFileName("Il2CppGenericInstDefinitions.c")))
				{
					return new Il2CppGenericInstWriter(writer).WriteIl2CppGenericInstDefinitions(_genericInstCollection);
				}
			}
		}

		private class WriteIl2CppGenericMethodTable : TableWriter
		{
			public WriteIl2CppGenericMethodTable(int index)
				: base(index)
			{
			}

			public override TableInfo Write(SourceWritingContext context)
			{
				using (IGeneratedCodeWriter writer = context.CreateProfiledGeneratedCodeSourceWriterInOutputDirectory(context.Global.Services.PathFactory.GetFileName("Il2CppGenericMethodTable.c")))
				{
					writer.AddCodeGenMetadataIncludes();
					return writer.WriteTable("const Il2CppGenericMethodFunctionsDefinitions", context.Global.Services.Naming.ForMetadataGlobalVar("g_Il2CppGenericMethodFunctions"), context.Global.Results.SecondaryCollection.MethodTables.SortedGenericMethodTableValues, (CollectMethodTables.GenericMethodTableEntry m) => FormatMethodTableEntry(context, m, context.Global.Results.SecondaryCollection.Invokers), externTable: true);
				}
			}
		}

		private class WriteIl2CppTypeDefinitions : TableWriter
		{
			public WriteIl2CppTypeDefinitions(int index)
				: base(index)
			{
			}

			public override TableInfo Write(SourceWritingContext context)
			{
				using (IGeneratedCodeWriter writer = context.CreateProfiledGeneratedCodeSourceWriterInOutputDirectory(context.Global.Services.PathFactory.GetFileName("Il2CppTypeDefinitions.c")))
				{
					return new Il2CppTypeWriter(writer).WriteIl2CppTypeDefinitions(context.Global.Results.SecondaryCollection.Metadata, context.Global.Results.SecondaryCollection.Types);
				}
			}
		}

		private class WriteIl2CppGenericMethodDefinitions : TableWriter
		{
			private readonly GenericInstancesCollection _genericInstCollection;

			public WriteIl2CppGenericMethodDefinitions(int index, GenericInstancesCollection genericInstCollection)
				: base(index)
			{
				_genericInstCollection = genericInstCollection;
			}

			public override TableInfo Write(SourceWritingContext context)
			{
				using (ICppCodeWriter writer = context.CreateProfiledSourceWriterInOutputDirectory(context.Global.Services.PathFactory.GetFileName("Il2CppGenericMethodDefinitions.c")))
				{
					return new Il2CppGenericMethodWriter(writer).WriteIl2CppGenericMethodDefinitions(context.Global.Results.SecondaryCollection.Metadata, _genericInstCollection, context.Global.PrimaryWriteResults.GenericMethods);
				}
			}
		}

		private class WriteCompilerCalculateTypeValues : TableWriter
		{
			public WriteCompilerCalculateTypeValues(int index)
				: base(index)
			{
			}

			public override TableInfo Write(SourceWritingContext context)
			{
				INamingService naming = context.Global.Services.Naming;
				IMetadataCollectionResults metadata = context.Global.Results.SecondaryCollection.Metadata;
				using (MiniProfiler.Section("CompilerCalculateTypeValues"))
				{
					using (ICppCodeWriter writer = context.CreateProfiledSourceWriterInOutputDirectory("Il2CppCCTypeValuesTable.cpp"))
					{
						ReadOnlyCollection<KeyValuePair<TypeDefinition, MetadataTypeDefinitionInfo>> typeInfos = metadata.GetTypeInfos();
						List<string> typeDefinitionSizeVarList = new List<string>(typeInfos.Count);
						string typeDefinitionSizeVarBaseName = naming.ForMetadataGlobalVar("g_typeDefinitionSize");
						SourceWriter.WriteEqualSizedChunks(context, typeInfos, "Il2CppCCalculateTypeValues", 1048576L, delegate(IGeneratedCodeWriter subwriter)
						{
							subwriter.WriteClangWarningDisables();
						}, delegate(IGeneratedCodeWriter subwriter, KeyValuePair<TypeDefinition, MetadataTypeDefinitionInfo> typeItem, NPath filePath)
						{
							var (type, metadataTypeDefinitionInfo2) = typeItem;
							subwriter.AddIncludeForTypeDefinition(type);
							string text = $"{typeDefinitionSizeVarBaseName}{metadataTypeDefinitionInfo2.Index}";
							typeDefinitionSizeVarList.Add(text);
							subwriter.WriteLine("extern const Il2CppTypeDefinitionSizes " + text + ";");
							subwriter.WriteLine("const Il2CppTypeDefinitionSizes " + text + " = { " + Sizes(context, type) + " };");
						}, delegate(IGeneratedCodeWriter subwriter)
						{
							subwriter.WriteClangWarningEnables();
						});
						return WriteTypeDefinitionSizesTable(naming, writer, typeDefinitionSizeVarList);
					}
				}
			}
		}

		private class WriteCompilerCalculateFieldValues : TableWriter
		{
			public WriteCompilerCalculateFieldValues(int index)
				: base(index)
			{
			}

			public override TableInfo Write(SourceWritingContext context)
			{
				INamingService naming = context.Global.Services.Naming;
				IMetadataCollectionResults metadata = context.Global.Results.SecondaryCollection.Metadata;
				using (MiniProfiler.Section("CompilerCalculateFieldValues"))
				{
					using (ICppCodeWriter writer = context.CreateProfiledSourceWriterInOutputDirectory("Il2CppCCFieldValuesTable.cpp"))
					{
						ReadOnlyCollection<KeyValuePair<TypeDefinition, MetadataTypeDefinitionInfo>> typeInfos = metadata.GetTypeInfos();
						List<TableInfo> fieldTableInfos = new List<TableInfo>(typeInfos.Count);
						string fieldOffsetTableBaseName = naming.ForMetadataGlobalVar("g_FieldOffsetTable");
						SourceWriter.WriteEqualSizedChunks(context, typeInfos, "Il2CppCCalculateFieldValues", 1048576L, delegate(IGeneratedCodeWriter subwriter)
						{
							subwriter.WriteClangWarningDisables();
						}, delegate(IGeneratedCodeWriter subwriter, KeyValuePair<TypeDefinition, MetadataTypeDefinitionInfo> typeItem, NPath filePath)
						{
							var (typeDefinition2, metadataTypeDefinitionInfo2) = typeItem;
							subwriter.AddIncludeForTypeDefinition(typeDefinition2);
							List<TableInfo> list = fieldTableInfos;
							string text = fieldOffsetTableBaseName;
							int index = metadataTypeDefinitionInfo2.Index;
							list.Add(subwriter.WriteTable("IL2CPP_EXTERN_C const int32_t", text + index, typeDefinition2.Fields, (FieldDefinition item) => OffsetOf(context, item), externTable: false));
						}, delegate(IGeneratedCodeWriter subwriter)
						{
							subwriter.WriteClangWarningEnables();
						});
						return WriteFieldTable(naming, writer, fieldTableInfos);
					}
				}
			}
		}

		private readonly SourceWritingContext _context;

		public MetadataCacheWriter(SourceWritingContext context)
		{
			_context = context;
		}

		public void Write(out IStringLiteralCollection stringLiteralCollection, out IFieldReferenceCollection fieldReferenceCollection)
		{
			WriteLibIl2CppMetadata(out stringLiteralCollection, out fieldReferenceCollection);
		}

		public void WriteLibIl2CppMetadata(out IStringLiteralCollection stringLiteralCollection, out IFieldReferenceCollection fieldReferenceCollection)
		{
			FieldReferenceCollector fieldReferenceCollector = new FieldReferenceCollector(_context);
			TableInfo metadataUsageTables = WriteIl2CppMetadataUsage(fieldReferenceCollector, out stringLiteralCollection);
			fieldReferenceCollection = fieldReferenceCollector;
			GenericInstancesCollection genericInstCollection;
			using (MiniProfiler.Section("Il2CppGenericInstCollectorComponent"))
			{
				genericInstCollection = Il2CppGenericInstCollectorComponent.Collect(_context);
			}
			List<TableWriter> list = new List<TableWriter>();
			list.Add(new WriteIl2CppGenericMethodTable(2));
			list.Add(new WriteIl2CppGenericClassTable(0));
			list.Add(new WriteIl2CppGenericInstDefinitions(1, genericInstCollection));
			list.Add(new WriteIl2CppTypeDefinitions(3));
			list.Add(new WriteIl2CppGenericMethodDefinitions(4, genericInstCollection));
			list.Add(new WriteCompilerCalculateFieldValues(5));
			list.Add(new WriteCompilerCalculateTypeValues(6));
			_context.Global.Services.Scheduler.EnqueueItemsAndContinueWithResults(_context.Global, list, (WorkItemData<GlobalWriteContext, TableWriter, object> workerData) => workerData.Item.Write(workerData.Context.CreateSourceWritingContext()), delegate(WorkItemData<GlobalWriteContext, ReadOnlyCollection<ResultData<TableWriter, TableInfo>>, object> workerData)
			{
				List<TableInfo> list2 = new List<TableInfo>();
				list2.AddRange(from d in workerData.Item.ToSortedCollection(new TableWriterComparer())
					select d.Result);
				list2.Add(metadataUsageTables);
				WriteIl2CppMetadataRegistration(workerData.Context.CreateSourceWritingContext(), list2);
			}, null);
		}

		private TableInfo WriteIl2CppMetadataUsage(IFieldReferenceCollector fieldReferenceCollector, out IStringLiteralCollection stringLiteralCollection)
		{
			using (ICppCodeWriter writer = _context.CreateProfiledSourceWriterInOutputDirectory(_context.Global.Services.PathFactory.GetFileName("Il2CppMetadataUsage.c")))
			{
				return new MetadataUsageWriter(_context, writer).WriteMetadataUsage(fieldReferenceCollector, _context.Global.Results.PrimaryWrite.MetadataUsage, out stringLiteralCollection);
			}
		}

		private static void WriteIl2CppMetadataRegistration(SourceWritingContext context, List<TableInfo> metadataInitializers)
		{
			using (ICppCodeWriter cppCodeWriter = context.CreateProfiledSourceWriterInOutputDirectory(context.Global.Services.PathFactory.GetFileName("Il2CppMetadataRegistration.c")))
			{
				cppCodeWriter.AddCodeGenMetadataIncludes();
				foreach (TableInfo item in metadataInitializers.Where((TableInfo i) => i.Count != 0))
				{
					cppCodeWriter.WriteLine("{0}{1} {2}[];", item.ExternTable ? "extern " : string.Empty, item.Type, item.Name);
				}
				cppCodeWriter.WriteStructInitializer("const Il2CppMetadataRegistration", RegistrationTableName(context), metadataInitializers.SelectMany(delegate(TableInfo table)
				{
					string[] array = new string[2];
					int count = table.Count;
					array[0] = count.ToString(CultureInfo.InvariantCulture);
					array[1] = table.Name;
					return array;
				}), externStruct: true);
			}
		}

		public static string RegistrationTableName(ReadOnlyContext context)
		{
			return context.Global.Services.Naming.ForMetadataGlobalVar("g_MetadataRegistration");
		}

		private static string FormatMethodTableEntry(SourceWritingContext context, CollectMethodTables.GenericMethodTableEntry m, ReadOnlyInvokerCollection invokerCollection)
		{
			string[] obj = new string[9] { "{ ", null, null, null, null, null, null, null, null };
			uint tableIndex = m.TableIndex;
			obj[1] = tableIndex.ToString();
			obj[2] = ", ";
			int pointerTableIndex = m.PointerTableIndex;
			obj[3] = pointerTableIndex.ToString();
			obj[4] = ", ";
			obj[5] = invokerCollection.GetIndex(context, m.Method.GenericMethod).ToString();
			obj[6] = ", ";
			pointerTableIndex = m.AdjustorThunkTableIndex;
			obj[7] = pointerTableIndex.ToString();
			obj[8] = "}";
			return string.Concat(obj);
		}

		public static TableInfo WriteFieldTable(INamingService namingService, ICppCodeWriter writer, List<TableInfo> fieldTableInfos)
		{
			TableInfo[] array = fieldTableInfos.Where((TableInfo item) => item.Count > 0).ToArray();
			for (int i = 0; i < array.Length; i++)
			{
				TableInfo tableInfo = array[i];
				writer.WriteLine("IL2CPP_EXTERN_C_CONST int32_t {0}[{1}];", tableInfo.Name, tableInfo.Count);
			}
			return writer.WriteTable("IL2CPP_EXTERN_C_CONST int32_t*", namingService.ForMetadataGlobalVar("g_FieldOffsetTable"), fieldTableInfos, (TableInfo table) => (table.Count <= 0) ? "NULL" : table.Name, externTable: false);
		}

		public static TableInfo WriteTypeDefinitionSizesTable(INamingService namingService, ICppCodeWriter writer, ICollection<string> typeDefintionSizeVarList)
		{
			foreach (string typeDefintionSizeVar in typeDefintionSizeVarList)
			{
				writer.WriteLine("extern const Il2CppTypeDefinitionSizes {0};", typeDefintionSizeVar);
			}
			return writer.WriteTable("IL2CPP_EXTERN_C_CONST Il2CppTypeDefinitionSizes*", namingService.ForMetadataGlobalVar("g_Il2CppTypeDefinitionSizesTable"), typeDefintionSizeVarList, Emit.AddressOf, externTable: false);
		}

		private static string Sizes(MinimalContext context, TypeDefinition type)
		{
			bool hasCompileTimeSize = !type.HasGenericParameters || type.ClassSize > 0;
			return string.Format("{0}, {1}, {2}, {3}", InstanceSizeFor(context, type, hasCompileTimeSize), NativeSizeFor(context, type, hasCompileTimeSize), (!type.HasGenericParameters && (type.Fields.Any((FieldDefinition f) => f.IsNormalStatic()) || type.StoresNonFieldsInStaticFields())) ? $"sizeof({context.Global.Services.Naming.ForStaticFieldsStruct(type)})" : "0", (!type.HasGenericParameters && type.Fields.Any((FieldDefinition f) => f.IsThreadStatic())) ? $"sizeof({context.Global.Services.Naming.ForThreadFieldsStruct(type)})" : "0");
		}

		private static string InstanceSizeFor(ReadOnlyContext context, TypeDefinition type, bool hasCompileTimeSize)
		{
			if (type.IsInterface() || !hasCompileTimeSize)
			{
				return "0";
			}
			if (type.HasGenericParameters)
			{
				return $"{type.ClassSize} + sizeof (RuntimeObject)";
			}
			return string.Format("sizeof ({0}){1}", context.Global.Services.Naming.ForType(type), type.IsValueType() ? "+ sizeof (RuntimeObject)" : string.Empty);
		}

		private static string NativeSizeFor(MinimalContext context, TypeDefinition type, bool hasCompileTimeSize)
		{
			if (!hasCompileTimeSize)
			{
				return "0";
			}
			if (type.HasGenericParameters)
			{
				return $"{type.ClassSize}";
			}
			return MarshalDataCollector.MarshalInfoWriterFor(context, type, MarshalType.PInvoke).NativeSize;
		}

		private static string OffsetOf(ReadOnlyContext context, FieldDefinition field)
		{
			if (field.IsLiteral)
			{
				return "0";
			}
			if (field.DeclaringType.HasGenericParameters)
			{
				if (field.IsThreadStatic())
				{
					return "THREAD_STATIC_FIELD_OFFSET";
				}
				return "0";
			}
			if (field.IsThreadStatic())
			{
				return $"{context.Global.Services.Naming.ForThreadFieldsStruct(field.DeclaringType)}::{context.Global.Services.Naming.ForFieldOffsetGetter(field)}() | THREAD_LOCAL_STATIC_MASK";
			}
			if (field.IsNormalStatic())
			{
				return $"{context.Global.Services.Naming.ForStaticFieldsStruct(field.DeclaringType)}::{context.Global.Services.Naming.ForFieldOffsetGetter(field)}()";
			}
			return string.Format("{0}::{1}(){2}", context.Global.Services.Naming.ForTypeNameOnly(field.DeclaringType), context.Global.Services.Naming.ForFieldOffsetGetter(field), field.DeclaringType.IsValueType() ? (" + static_cast<int32_t>(sizeof(" + context.Global.Services.Naming.ForType(context.Global.Services.TypeProvider.SystemObject) + "))") : "");
		}
	}
}
