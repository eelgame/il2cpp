using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Collections;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Metadata.RuntimeTypes;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.Metadata
{
	public class MonoGenericInstMetadataWriter : MetadataWriter<ICppCodeWriter>
	{
		public const string TableVariableName = "g_MonoGenericInstMetadataTable";

		private readonly SourceWritingContext _context;

		public MonoGenericInstMetadataWriter(SourceWritingContext context, ICppCodeWriter writer)
			: base(writer)
		{
			_context = context;
		}

		private List<IIl2CppRuntimeType[]> OrderInstances(GenericInstancesCollection genericInstCollection)
		{
			HashSet<List<TypeReference>> hashSet = new HashSet<List<TypeReference>>(new Il2CppGenericInstComparer());
			List<IIl2CppRuntimeType[]> list = new List<IIl2CppRuntimeType[]>();
			List<IIl2CppRuntimeType[]> list2 = genericInstCollection.Select((KeyValuePair<IIl2CppRuntimeType[], uint> item) => item.Key).ToList();
			while (list2.Count > 0)
			{
				int num = 0;
				while (num < list2.Count)
				{
					IIl2CppRuntimeType[] array = list2[num];
					bool flag = true;
					IIl2CppRuntimeType[] array2 = array;
					for (int i = 0; i < array2.Length; i++)
					{
						if (array2[i].Type is GenericInstanceType genericInstanceType && !hashSet.Contains(genericInstanceType.GenericArguments.ToList()))
						{
							flag = false;
							break;
						}
					}
					if (flag)
					{
						list.Add(array);
						list2.RemoveAt(num);
						hashSet.Add(array.Select((IIl2CppRuntimeType t) => t.Type).ToList());
					}
					else
					{
						num++;
					}
				}
			}
			return list;
		}

		private string GenericInstName(IList<IIl2CppRuntimeType> inst)
		{
			return "Mono" + _context.Global.Services.Naming.ForGenericInst(inst);
		}

		public TableInfo WriteMonoMetadataForGenericInstances(GenericInstancesCollection genericInstCollection)
		{
			base.Writer.AddInclude("il2cpp-mapping.h");
			foreach (IIl2CppRuntimeType[] item in OrderInstances(genericInstCollection))
			{
				WriteLine("static const TypeIndex {0}[] = {{ {1} }};", GenericInstName(item) + "_Types", item.Select((IIl2CppRuntimeType typeRef) => _context.Global.Results.SecondaryCollection.Types.GetIndex(typeRef).ToString()).AggregateWithComma());
				WriteLine("extern const MonoGenericInstMetadata {0} = {{ {1}, {2} }};", GenericInstName(item), item.Length, GenericInstName(item) + "_Types");
			}
			return base.Writer.WriteTable("const MonoGenericInstMetadata* const", "g_MonoGenericInstMetadataTable", genericInstCollection.ItemsSortedByValue(), (KeyValuePair<IIl2CppRuntimeType[], uint> item) => "&" + GenericInstName(item.Key), externTable: true);
		}
	}
}
