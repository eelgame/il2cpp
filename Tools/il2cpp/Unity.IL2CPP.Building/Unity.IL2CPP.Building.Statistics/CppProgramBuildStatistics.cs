namespace Unity.IL2CPP.Building.Statistics
{
	public class CppProgramBuildStatistics : IBuildStatistics, IBuildStatisticsCollector
	{
		private int _totalFiles;

		private int _cacheHits;

		public int TotalFiles => _totalFiles;

		public int CacheHits => _cacheHits;

		public int CacheHitPercentage
		{
			get
			{
				if (TotalFiles != 0)
				{
					return (int)((double)CacheHits / (double)TotalFiles * 100.0);
				}
				return 0;
			}
		}

		public int FilesCompiled => TotalFiles - CacheHits;

		public CppProgramBuildStatistics()
		{
		}

		public CppProgramBuildStatistics(int totalFiles, int cacheHits)
		{
			_totalFiles = totalFiles;
			_cacheHits = cacheHits;
		}

		void IBuildStatisticsCollector.IncrementTotalFileCountBy(int amount)
		{
			_totalFiles += amount;
		}

		void IBuildStatisticsCollector.IncrementCacheHitCountBy(int amount)
		{
			_cacheHits += amount;
		}
	}
}
