namespace Unity.IL2CPP.Contexts
{
	public class MetadataWriteContext
	{
		public readonly GlobalMetadataWriteContext Global;

		public MetadataWriteContext(GlobalMetadataWriteContext context)
		{
			Global = context;
		}

		public ReadOnlyContext AsReadonly()
		{
			return Global.GetReadOnlyContext();
		}

		public static implicit operator ReadOnlyContext(MetadataWriteContext d)
		{
			return d.AsReadonly();
		}
	}
}
