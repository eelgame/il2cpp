using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Collections;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.Metadata.RuntimeTypes;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.Metadata
{
	public class Il2CppGenericInstWriter : MetadataWriter<IGeneratedCodeWriter>
	{
		private readonly SourceWritingContext _context;

		public Il2CppGenericInstWriter(IGeneratedCodeWriter writer)
			: base(writer)
		{
			_context = writer.Context;
		}

		public TableInfo WriteIl2CppGenericInstDefinitions(GenericInstancesCollection genericInstCollection)
		{
			base.Writer.AddCodeGenMetadataIncludes();
			INamingService naming = _context.Global.Services.Naming;
			ReadOnlyCollection<KeyValuePair<IIl2CppRuntimeType[], uint>> readOnlyCollection = genericInstCollection.ItemsSortedByValue();
			foreach (IIl2CppRuntimeType[] item in readOnlyCollection.Select((KeyValuePair<IIl2CppRuntimeType[], uint> item) => item.Key))
			{
				for (int i = 0; i < item.Length; i++)
				{
					base.Writer.WriteExternForIl2CppType(item[i]);
				}
				WriteLine("static const Il2CppType* {0}[] = {{ {1} }};", naming.ForGenericInst(item) + "_Types", item.Select((IIl2CppRuntimeType t) => MetadataUtils.TypeRepositoryTypeFor(_context, t)).AggregateWithComma());
				WriteLine("extern const Il2CppGenericInst {0};", naming.ForGenericInst(item));
				WriteLine("const Il2CppGenericInst {0} = {{ {1}, {2} }};", naming.ForGenericInst(item), item.Length, naming.ForGenericInst(item) + "_Types");
			}
			return base.Writer.WriteTable("const Il2CppGenericInst* const", naming.ForMetadataGlobalVar("g_Il2CppGenericInstTable"), readOnlyCollection, (KeyValuePair<IIl2CppRuntimeType[], uint> item) => "&" + naming.ForGenericInst(item.Key), externTable: true);
		}
	}
}
