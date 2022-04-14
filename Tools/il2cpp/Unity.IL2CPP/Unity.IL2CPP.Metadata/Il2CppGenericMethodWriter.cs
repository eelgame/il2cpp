using System.Collections.Generic;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Collections;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Results;

namespace Unity.IL2CPP.Metadata
{
	public class Il2CppGenericMethodWriter : MetadataWriter<ICppCodeWriter>
	{
		private readonly ReadOnlyContext _context;

		public Il2CppGenericMethodWriter(ICppCodeWriter writer)
			: base(writer)
		{
			_context = writer.Context;
		}

		public TableInfo WriteIl2CppGenericMethodDefinitions(IMetadataCollectionResults metadataCollection, GenericInstancesCollection genericInstCollection, IGenericMethodCollectorResults genericMethods)
		{
			base.Writer.AddCodeGenMetadataIncludes();
			return base.Writer.WriteTable("const Il2CppMethodSpec", _context.Global.Services.Naming.ForMetadataGlobalVar("g_Il2CppMethodSpecTable"), genericMethods.Table.ItemsSortedByValue(), (KeyValuePair<Il2CppMethodSpec, uint> item) => FormatGenericMethod(item.Key, metadataCollection, genericInstCollection), externTable: true);
		}

		private string FormatGenericMethod(Il2CppMethodSpec methodSpec, IMetadataCollectionResults metadataCollection, GenericInstancesCollection genericInstCollection)
		{
			return $"{{ {metadataCollection.GetMethodIndex(methodSpec.GenericMethod.Resolve())}, {((methodSpec.TypeGenericInstanceData != null) ? ((int)genericInstCollection[methodSpec.TypeGenericInstanceData]) : (-1))}, {((methodSpec.MethodGenericInstanceData != null) ? ((int)genericInstCollection[methodSpec.MethodGenericInstanceData]) : (-1))} }}";
		}
	}
}
