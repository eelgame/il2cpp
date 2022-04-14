namespace Unity.IL2CPP.Building.Statistics
{
	public interface IBuildStatistics
	{
		int TotalFiles { get; }

		int FilesCompiled { get; }

		int CacheHits { get; }

		int CacheHitPercentage { get; }
	}
}
