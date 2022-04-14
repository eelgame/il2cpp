namespace Unity.IL2CPP.Attributes
{
	public struct AttributeClassTypeRange
	{
		public readonly uint MetadataToken;

		public readonly int Start;

		public readonly int Count;

		public AttributeClassTypeRange(uint metadataToken, int start, int count)
		{
			MetadataToken = metadataToken;
			Start = start;
			Count = count;
		}
	}
}
