using System.Collections.ObjectModel;
using Unity.IL2CPP.AssemblyConversion.SecondaryCollection.Steps.Generics;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Metadata;

namespace Unity.IL2CPP
{
	public sealed class MethodTableWriter
	{
		private readonly ICppCodeWriter _writer;

		public MethodTableWriter(ICppCodeWriter writer)
		{
			_writer = writer;
		}

		public TableInfo Write(SourceWritingContext context, ReadOnlyMethodTables methodTables)
		{
			WriteIncludesFor(context, _writer, methodTables.SortedGenericMethodPointerTableValues);
			return _writer.WriteTable("const Il2CppMethodPointer", context.Global.Services.Naming.ForMetadataGlobalVar("g_Il2CppGenericMethodPointers"), methodTables.SortedGenericMethodPointerTableValues, delegate(CollectMethodTables.GenericMethodPointerTableEntry m)
			{
				string text = (m.IsNull ? "NULL" : ("(Il2CppMethodPointer)&" + m.Name(context)));
				if (context.Global.Parameters.EmitComments)
				{
					int index = m.Index;
					return text + "/* " + index + "*/";
				}
				return text;
			}, externTable: true);
		}

		private static void WriteIncludesFor(SourceWritingContext context, ICppCodeWriter writer, ReadOnlyCollection<CollectMethodTables.GenericMethodPointerTableEntry> sortedTableEntries)
		{
			foreach (CollectMethodTables.GenericMethodPointerTableEntry sortedTableEntry in sortedTableEntries)
			{
				if (!sortedTableEntry.IsNull)
				{
					writer.WriteLine("IL2CPP_EXTERN_C void {0} ();", sortedTableEntry.Name(context));
				}
			}
		}
	}
}
