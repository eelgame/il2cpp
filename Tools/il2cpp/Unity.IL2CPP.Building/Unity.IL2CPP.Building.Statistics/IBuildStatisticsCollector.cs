namespace Unity.IL2CPP.Building.Statistics
{
	internal interface IBuildStatisticsCollector
	{
		void IncrementTotalFileCountBy(int amount);

		void IncrementCacheHitCountBy(int amount);
	}
}
