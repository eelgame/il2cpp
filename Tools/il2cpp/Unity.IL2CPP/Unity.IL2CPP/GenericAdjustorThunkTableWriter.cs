using System.Linq;
using Unity.IL2CPP.AssemblyConversion.SecondaryCollection.Steps.Generics;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Metadata;

namespace Unity.IL2CPP
{
	public class GenericAdjustorThunkTableWriter
	{
		private readonly ICppCodeWriter _writer;

		public GenericAdjustorThunkTableWriter(ICppCodeWriter writer)
		{
			_writer = writer;
		}

		public TableInfo Write(SourceWritingContext context)
		{
			string[] array = context.Global.Results.SecondaryCollection.MethodTables.SortedGenericMethodAdjustorThunkTableValues.Select((CollectMethodTables.GenericMethodAdjustorThunkTableEntry i) => i.Name(context)).ToArray();
			string[] array2 = array;
			foreach (string text in array2)
			{
				_writer.WriteLine("IL2CPP_EXTERN_C void {0} ();", text);
			}
			return _writer.WriteTable("const Il2CppMethodPointer", "g_Il2CppGenericAdjustorThunks", array, (string adjustorThunkName) => adjustorThunkName, externTable: true);
		}
	}
}
