namespace Unity.IL2CPP.Contexts.Collectors
{
	public interface IMetadataUsageCollectorWriterService
	{
		void Add(string identifier, MethodMetadataUsage usage);
	}
}
