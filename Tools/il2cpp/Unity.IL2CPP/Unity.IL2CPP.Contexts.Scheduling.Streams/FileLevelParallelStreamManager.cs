using System.Collections.ObjectModel;
using NiceIO;

namespace Unity.IL2CPP.Contexts.Scheduling.Streams
{
	public class FileLevelParallelStreamManager<TItem, TStream> : BaseParallelStreamManager<TItem, TStream> where TStream : IStream
	{
		private class FileLevelTag
		{
			public readonly ReadOnlyCollection<TItem> Chunk;

			public readonly SharedTag SharedTag;

			public readonly NPath ChunkFilePath;

			public FileLevelTag(ReadOnlyCollection<TItem> chunk, SharedTag sharedTag, NPath chunkFilePath)
			{
				Chunk = chunk;
				SharedTag = sharedTag;
				ChunkFilePath = chunkFilePath;
			}
		}

		public FileLevelParallelStreamManager(NPath outputDirectory, NPath baseFileName, IStreamWriterCallbacks<TItem, TStream> writerCallbacks)
			: base(outputDirectory, baseFileName, writerCallbacks)
		{
		}

		protected override void ProcessChunk(GlobalWriteContext context, ReadOnlyCollection<TItem> chunk, NPath chunkFilePath, SharedTag sharedTag)
		{
			context.Services.Scheduler.Enqueue(context, WorkerWriteItemsToFile, new FileLevelTag(chunk, sharedTag, chunkFilePath));
		}

		private static void WorkerWriteItemsToFile(WorkItemData<GlobalWriteContext, FileLevelTag> data)
		{
			FileLevelTag tag = data.Tag;
			IStreamWriterCallbacks<TItem, TStream> callbacks = tag.SharedTag.Callbacks;
			SourceWritingContext context = data.Context.CreateSourceWritingContext();
			using (TStream stream = BaseStreamManager<TItem, TStream>.GetAvailableStream(context, tag.SharedTag.Callbacks))
			{
				foreach (TItem item in tag.Chunk)
				{
					callbacks.WriteItem(new StreamWorkItemData<TItem, TStream>(context, item, stream, tag.ChunkFilePath));
				}
				tag.SharedTag.Callbacks.FlushStream(data.Context, stream, tag.ChunkFilePath);
			}
		}
	}
}
