using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Mono.Cecil;
using Unity.Cecil.Awesome.Comparers;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Results;
using Unity.IL2CPP.Metadata;
using Unity.IL2CPP.Metadata.RuntimeTypes;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP
{
	public class Il2CppTypeWriter : MetadataWriter<IGeneratedCodeWriter>
	{
		private class Il2CppTypeWriterRuntimeTypeComparer : IComparer<IIl2CppRuntimeType>
		{
			private readonly Il2CppRuntimeTypeComparer _elementComparer = new Il2CppRuntimeTypeComparer();

			public int Compare(IIl2CppRuntimeType x, IIl2CppRuntimeType y)
			{
				TypeReference nonPinnedAndNonByReferenceType = x.Type.GetNonPinnedAndNonByReferenceType();
				TypeReference nonPinnedAndNonByReferenceType2 = y.Type.GetNonPinnedAndNonByReferenceType();
				if (nonPinnedAndNonByReferenceType.IsGenericInstance && !nonPinnedAndNonByReferenceType2.IsGenericInstance)
				{
					return 1;
				}
				if (!nonPinnedAndNonByReferenceType.IsGenericInstance && nonPinnedAndNonByReferenceType2.IsGenericInstance)
				{
					return -1;
				}
				return _elementComparer.Compare(x, y);
			}
		}

		private ReadOnlyContext _context;

		public Il2CppTypeWriter(IGeneratedCodeWriter writer)
			: base(writer)
		{
			_context = writer.Context;
		}

		public TableInfo WriteIl2CppTypeDefinitions(IMetadataCollectionResults metadataCollection, ITypeCollectorResults types)
		{
			base.Writer.AddCodeGenMetadataIncludes();
			foreach (IGrouping<TypeReference, IIl2CppRuntimeType> item in types.SortedItems.ToSortedCollection(new Il2CppTypeWriterRuntimeTypeComparer()).GroupBy((IIl2CppRuntimeType entry) => entry.Type.GetNonPinnedAndNonByReferenceType(), new TypeReferenceEqualityComparer()))
			{
				base.Writer.WriteLine();
				TypeReference type = item.Key;
				IIl2CppRuntimeType il2CppRuntimeType = item.First((IIl2CppRuntimeType t) => TypeReferenceEqualityComparer.AreEqual(t.Type, type));
				GenericParameter genericParameter = type as GenericParameter;
				Il2CppGenericInstanceRuntimeType il2CppGenericInstanceRuntimeType = il2CppRuntimeType as Il2CppGenericInstanceRuntimeType;
				Il2CppArrayRuntimeType il2CppArrayRuntimeType = il2CppRuntimeType as Il2CppArrayRuntimeType;
				Il2CppPtrRuntimeType il2CppPtrRuntimeType = il2CppRuntimeType as Il2CppPtrRuntimeType;
				string text = ((genericParameter != null) ? GetMetadataIndex(genericParameter, metadataCollection.GetGenericParameterIndex) : ((il2CppGenericInstanceRuntimeType != null) ? WriteGenericInstanceTypeDataValue(il2CppGenericInstanceRuntimeType) : ((il2CppArrayRuntimeType != null) ? WriteArrayDataValue(il2CppArrayRuntimeType) : ((il2CppPtrRuntimeType == null) ? GetMetadataIndex(type.Resolve(), metadataCollection.GetTypeInfoIndex) : WritePointerDataValue(il2CppPtrRuntimeType)))));
				foreach (IIl2CppRuntimeType item2 in item)
				{
					if (!IncludeTypeDefinitionInContext(item2))
					{
						base.Writer.WriteExternForIl2CppType(item2);
						continue;
					}
					string text2 = _context.Global.Services.Naming.ForIl2CppType(item2);
					string text3 = Il2CppTypeSupport.DeclarationFor(item2.Type);
					base.Writer.WriteLine("extern {0} {1};", text3, text2);
					base.Writer.WriteLine("{0} {1} = {{ {2}, {3}, {4}, {5}, {6}, {7} }};", text3, text2, text, item2.Attrs.ToString(CultureInfo.InvariantCulture), Il2CppTypeSupport.For(item2.Type), "0", item2.Type.IsByReference ? "1" : "0", item2.Type.IsPinned ? "1" : "0");
				}
			}
			return base.Writer.WriteTable("const Il2CppType* const ", _context.Global.Services.Naming.ForMetadataGlobalVar("g_Il2CppTypeTable"), types.SortedItems, (IIl2CppRuntimeType runtimeType) => "&" + _context.Global.Services.Naming.ForIl2CppType(runtimeType), externTable: true);
		}

		private string GetMetadataIndex<T>(T type, Func<T, int> getIndex) where T : TypeReference
		{
			if (!_context.Global.Services.ContextScope.IncludeTypeDefinitionInContext(type))
			{
				return "0";
			}
			return "(void*)" + getIndex(type);
		}

		private bool IncludeTypeDefinitionInContext(IIl2CppRuntimeType runtimeType)
		{
			if (runtimeType.Attrs == 0 || runtimeType.Type.GetNonPinnedAndNonByReferenceType().IsGenericParameter)
			{
				return _context.Global.Services.ContextScope.IncludeTypeDefinitionInContext(runtimeType.Type);
			}
			return true;
		}

		private string WritePointerDataValue(Il2CppPtrRuntimeType pointerType)
		{
			base.Writer.WriteExternForIl2CppType(pointerType.ElementType);
			return "(void*)&" + _context.Global.Services.Naming.ForIl2CppType(pointerType.ElementType);
		}

		private string WriteGenericInstanceTypeDataValue(Il2CppGenericInstanceRuntimeType genericInstanceRuntimeType)
		{
			new GenericClassWriter(base.Writer).WriteDefinition(_context, genericInstanceRuntimeType);
			return "&" + _context.Global.Services.Naming.ForGenericClass(genericInstanceRuntimeType.Type);
		}

		private string WriteArrayDataValue(Il2CppArrayRuntimeType arrayType)
		{
			base.Writer.WriteExternForIl2CppType(arrayType.ElementType);
			if (arrayType.Type.Rank == 1)
			{
				return "(void*)&" + _context.Global.Services.Naming.ForIl2CppType(arrayType.ElementType);
			}
			string text = _context.Global.Services.Naming.ForRuntimeArrayType(arrayType);
			WriteLine("Il2CppArrayType {0} = ", text);
			WriteArrayInitializer(new string[6]
			{
				$"&{_context.Global.Services.Naming.ForIl2CppType(arrayType.ElementType)}",
				arrayType.Type.Rank.ToString(),
				"0",
				"0",
				"NULL",
				"NULL"
			});
			return "&" + text;
		}
	}
}
