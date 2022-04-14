using System;
using System.Collections.Generic;
using Unity.IL2CPP.CodeWriters;

namespace Unity.IL2CPP.Metadata
{
	internal static class MetadataWriterCodeWritingExtensions
	{
		internal static TableInfo WriteTable<T>(this ICppCodeWriter writer, string type, string name, ICollection<T> items, Func<T, string> map, bool externTable)
		{
			if (items.Count == 0)
			{
				return TableInfo.Empty;
			}
			writer.WriteArrayInitializer(type, name, items, map, externTable);
			return new TableInfo(items.Count, type, name, externTable);
		}
	}
}
