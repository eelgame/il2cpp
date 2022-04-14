using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using NiceIO;
using Unity.IL2CPP.Contexts.Services;

namespace Unity.IL2CPP.Contexts.Scheduling.Streams
{
	public class ItemLevelParallelStreamManager<TItem, TStream> : BaseParallelStreamManager<TItem, TStream> where TStream : IStream
	{
		private class ItemLevelParallelTag
		{
			public readonly NPath ChunkFilePath;

			public readonly SharedTag SharedTag;

			public ItemLevelParallelTag(SharedTag sharedTag, NPath chunkFilePath)
			{
				SharedTag = sharedTag;
				ChunkFilePath = chunkFilePath;
			}
		}

		private class ResultOrdererByItem : IComparer<ResultData<TItem, TStream>>
		{
			private readonly IComparer<TItem> _comparer;

			public ResultOrdererByItem(IComparer<TItem> comparer)
			{
				_comparer = comparer;
			}

			public int Compare(ResultData<TItem, TStream> x, ResultData<TItem, TStream> y)
			{
				return _comparer.Compare(x.Item, y.Item);
			}
		}

		public ItemLevelParallelStreamManager(NPath outputDirectory, NPath baseFileName, IStreamWriterCallbacks<TItem, TStream> writerCallbacks)
			: base(outputDirectory, baseFileName, writerCallbacks)
		{
		}

		protected override void ProcessChunk(GlobalWriteContext context, ReadOnlyCollection<TItem> chunk, NPath chunkFilePath, SharedTag sharedTag)
		{
			context.Services.Scheduler.EnqueueItemsAndContinueWithResults(context, chunk, WorkerWriteItemToStream, WorkerSortAndWriteStreamsToFile, new ItemLevelParallelTag(sharedTag, chunkFilePath));
		}

		private static TStream WorkerWriteItemToStream(WorkItemData<GlobalWriteContext, TItem, ItemLevelParallelTag> data)
		{
			IStreamWriterCallbacks<TItem, TStream> callbacks = data.Tag.SharedTag.Callbacks;
			SourceWritingContext context = data.Context.CreateSourceWritingContext();
			TStream availableStream = BaseStreamManager<TItem, TStream>.GetAvailableStream(context, data.Tag.SharedTag.Callbacks);
			try
			{
				callbacks.WriteItem(new StreamWorkItemData<TItem, TStream>(context, data.Item, availableStream, data.Tag.ChunkFilePath));
				return availableStream;
			}
			catch (Exception)
			{
				availableStream.Dispose();
				throw;
			}
		}

		private static void WorkerSortAndWriteStreamsToFile(WorkItemData<GlobalWriteContext, ReadOnlyCollection<ResultData<TItem, TStream>>, ItemLevelParallelTag> data)
		{
			data.Item.ToList().Sort(new ResultOrdererByItem(data.Tag.SharedTag.Callbacks.CreateComparer()));
			try
			{
				data.Tag.SharedTag.Callbacks.MergeAndFlushStreams(data.Context, data.Item, data.Tag.ChunkFilePath);
			}
			finally
			{
				foreach (ResultData<TItem, TStream> item in data.Item)
				{
					TStream result = item.Result;
					result.Dispose();
				}
			}
		}
	}
}
