using NiceIO;

namespace Unity.IL2CPP.Contexts.Scheduling.Streams
{
	public static class ScheduledStreamFactory
	{
		private static bool FavorItemLevelOverFile => false;

		public static BaseStreamManager<TItem, TStream> Create<TItem, TStream>(SourceWritingContext context, NPath fileName, IStreamWriterCallbacks<TItem, TStream> callbacks) where TStream : IStream
		{
			NPath outputDir = context.Global.InputData.OutputDir;
			if (context.Global.Parameters.EnableSerialConversion)
			{
				return new SerialStreamManager<TItem, TStream>(outputDir, fileName, callbacks);
			}
			if (FavorItemLevelOverFile)
			{
				return new ItemLevelParallelStreamManager<TItem, TStream>(outputDir, fileName, callbacks);
			}
			return new FileLevelParallelStreamManager<TItem, TStream>(outputDir, fileName, callbacks);
		}
	}
}
