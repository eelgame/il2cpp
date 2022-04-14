namespace Unity.IL2CPP.Tiny
{
	public struct StringLiteralEntry
	{
		public readonly uint Offset32;

		public readonly ulong Offset64;

		public readonly string OffsetConstantName;

		public StringLiteralEntry(uint offset32, ulong offset64, string offsetConstantName)
		{
			Offset32 = offset32;
			Offset64 = offset64;
			OffsetConstantName = offsetConstantName;
		}
	}
}
