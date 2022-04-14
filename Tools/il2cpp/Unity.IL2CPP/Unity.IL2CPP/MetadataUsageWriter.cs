using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using Mono.Cecil;
using Unity.Cecil.Awesome.Comparers;
using Unity.Cecil.Awesome.Ordering;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Results;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.Marshaling.BodyWriters.NativeToManaged;
using Unity.IL2CPP.Metadata;
using Unity.IL2CPP.Metadata.RuntimeTypes;
using Unity.IL2CPP.Naming;
using Unity.IL2CPP.StringLiterals;

namespace Unity.IL2CPP
{
	public class MetadataUsageWriter : MetadataWriter<ICppCodeWriter>
	{
		private readonly SourceWritingContext _context;

		public MetadataUsageWriter(SourceWritingContext context, ICppCodeWriter writer)
			: base(writer)
		{
			_context = context;
		}

		public TableInfo WriteMetadataUsage(IFieldReferenceCollector fieldReferenceCollector, IMetadataUsageCollectorResults metadataUsages, out IStringLiteralCollection stringLiteralCollection)
		{
			base.Writer.AddCodeGenMetadataIncludes();
			INamingService naming = _context.Global.Services.Naming;
			List<Tuple<string, uint>> list = null;
			if (_context.Global.Parameters.EnableDebugger || _context.Global.Parameters.EnableReload)
			{
				list = new List<Tuple<string, uint>>(metadataUsages.UsageCount);
			}
			foreach (IIl2CppRuntimeType item in metadataUsages.GetIl2CppTypes().ToSortedCollection())
			{
				base.Writer.WriteStatement(BuildMetadataInitStatement(list, "RuntimeType", naming.ForRuntimeIl2CppType(item), MetadataUtils.GetEncodedMetadataUsageIndex((uint)_context.Global.Results.SecondaryCollection.Types.GetIndex(item), Il2CppMetadataUsage.Il2CppType)));
			}
			foreach (IIl2CppRuntimeType item2 in metadataUsages.GetTypeInfos().ToSortedCollection())
			{
				base.Writer.WriteStatement(BuildMetadataInitStatement(list, "RuntimeClass", naming.ForRuntimeTypeInfo(item2), MetadataUtils.GetEncodedMetadataUsageIndex((uint)_context.Global.Results.SecondaryCollection.Types.GetIndex(item2), Il2CppMetadataUsage.Il2CppClass)));
			}
			foreach (MethodReference item3 in AllMethodsThatNeedRuntimeMetadata(metadataUsages).ToSortedCollection())
			{
				base.Writer.WriteStatement(BuildMetadataInitStatement(list, "RuntimeMethod", naming.ForRuntimeMethodInfo(item3), MetadataUtils.GetEncodedMethodMetadataUsageIndex(item3, _context.Global.SecondaryCollectionResults.Metadata, _context.Global.PrimaryWriteResults.GenericMethods)));
			}
			foreach (Il2CppRuntimeFieldReference item4 in metadataUsages.GetFieldInfos().ToSortedCollection())
			{
				base.Writer.WriteStatement(BuildMetadataInitStatement(list, "RuntimeField", naming.ForRuntimeFieldInfo(item4), MetadataUtils.GetEncodedMetadataUsageIndex(fieldReferenceCollector.GetOrCreateIndex(item4), Il2CppMetadataUsage.FieldInfo)));
			}
			StringLiteralCollection stringLiteralCollection2 = new StringLiteralCollection();
			foreach (StringMetadataToken item5 in metadataUsages.GetStringLiterals().ToSortedCollection())
			{
				base.Writer.WriteStatement(BuildMetadataInitStatement(list, "String_t", naming.ForRuntimeUniqueStringLiteralIdentifier(item5.Literal), MetadataUtils.GetEncodedMetadataUsageIndex((uint)stringLiteralCollection2.Add(item5), Il2CppMetadataUsage.StringLiteral)));
			}
			stringLiteralCollection2.Complete();
			stringLiteralCollection = stringLiteralCollection2;
			TableInfo result = TableInfo.Empty;
			if (_context.Global.Parameters.EnableDebugger || _context.Global.Parameters.EnableReload)
			{
				result = base.Writer.WriteTable("void** const", _context.Global.Services.Naming.ForMetadataGlobalVar("g_MetadataUsages"), list, (Tuple<string, uint> x) => "(void**)&" + x.Item1, externTable: true);
			}
			if (_context.Global.Parameters.EnableReload)
			{
				base.Writer.WriteLine("#if IL2CPP_ENABLE_RELOAD");
				TableInfo tableInfo = base.Writer.WriteTable("uint32_t", "s_MetaDataTokenReload", list, (Tuple<string, uint> x) => x.Item2.ToString(CultureInfo.InvariantCulture), externTable: false);
				int num = 0;
				ReadOnlyCollection<KeyValuePair<string, MethodMetadataUsage>> readOnlyCollection = metadataUsages.GetUsages().ItemsSortedByKey();
				foreach (KeyValuePair<string, MethodMetadataUsage> item6 in readOnlyCollection)
				{
					base.Writer.WriteStatement($"extern const uint32_t {item6.Key}");
					base.Writer.WriteStatement($"const uint32_t {item6.Key} = {num++}");
				}
				string text = _context.Global.Services.Naming.ForReloadMethodMetadataInitialized();
				base.Writer.WriteStatement($"bool {text}[{readOnlyCollection.Count}]");
				base.Writer.WriteLine("void ClearMethodMetadataInitializedFlags()");
				base.Writer.BeginBlock();
				base.Writer.WriteStatement("memset(" + text + ", 0, sizeof(" + text + "))");
				base.Writer.WriteLine();
				base.Writer.WriteLine($"for(int32_t i = 0; i < {list.Count}; i++)");
				base.Writer.Indent();
				base.Writer.WriteStatement("*(uintptr_t*)" + result.Name + "[i] = (uintptr_t)" + tableInfo.Name + "[i]");
				base.Writer.Dedent();
				base.Writer.EndBlock();
				base.Writer.WriteLine("#endif");
			}
			return result;
		}

		private HashSet<MethodReference> AllMethodsThatNeedRuntimeMetadata(IMetadataUsageCollectorResults metadataUsages)
		{
			HashSet<MethodReference> hashSet = new HashSet<MethodReference>(new MethodReferenceComparer());
			hashSet.UnionWith(metadataUsages.GetInflatedMethods());
			hashSet.UnionWith(_context.Global.Results.PrimaryWrite.ReversePInvokeWrappers.SortedKeys.Where(ReversePInvokeMethodBodyWriter.IsReversePInvokeMethodThatMustBeGenerated));
			return hashSet;
		}

		private static string BuildMetadataInitStatement(List<Tuple<string, uint>> tokens, string type, string name, uint token)
		{
			tokens?.Add(new Tuple<string, uint>(name, token));
			return $"{type}* {name} = ({type}*)(uintptr_t){token}";
		}
	}
}
