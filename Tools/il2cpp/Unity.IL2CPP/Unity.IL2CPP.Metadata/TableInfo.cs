namespace Unity.IL2CPP.Metadata
{
	public struct TableInfo
	{
		public readonly int Count;

		public readonly string Type;

		public readonly string Name;

		public readonly bool ExternTable;

		public static TableInfo Empty => new TableInfo(0, "NULL", "NULL", externTable: false);

		public TableInfo(int count, string type, string name, bool externTable)
		{
			Count = count;
			Type = type;
			Name = name;
			ExternTable = externTable;
		}

		public string GetDeclaration()
		{
			return (ExternTable ? "IL2CPP_EXTERN_C " : string.Empty) + Type + " " + Name + "[];";
		}
	}
}
