namespace Unity.IL2CPP.Contexts.Components.Base
{
	public interface IMetadataIndexTableResults<TItem> : ITableResults<TItem, uint>
	{
		uint GetIndex(TItem key);

		bool HasIndex(TItem key);
	}
}
