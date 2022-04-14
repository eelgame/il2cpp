namespace Unity.IL2CPP.Contexts.Forking.Providers
{
	public interface IAllContextsProvider
	{
		GlobalWriteContext GlobalWriteContext { get; }

		GlobalPrimaryCollectionContext GlobalPrimaryCollectionContext { get; }

		GlobalSecondaryCollectionContext GlobalSecondaryCollectionContext { get; }

		GlobalMetadataWriteContext GlobalMetadataWriteContext { get; }

		TinyGlobalWriteContext TinyGlobalWriteContext { get; }

		GlobalReadOnlyContext GlobalReadOnlyContext { get; }

		GlobalMinimalContext GlobalMinimalContext { get; }
	}
}
