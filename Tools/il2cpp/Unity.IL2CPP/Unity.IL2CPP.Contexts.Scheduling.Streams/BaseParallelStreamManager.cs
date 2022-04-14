using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using NiceIO;

namespace Unity.IL2CPP.Contexts.Scheduling.Streams
{
	public abstract class BaseParallelStreamManager<TItem, TStream> : BaseStreamManager<TItem, TStream> where TStream : IStream
	{
		protected BaseParallelStreamManager(NPath outputDirectory, NPath baseFileName, IStreamWriterCallbacks<TItem, TStream> writerCallbacks)
			: base(outputDirectory, baseFileName, writerCallbacks)
		{
		}

		public override void Write(GlobalWriteContext context, ICollection<TItem> items)
		{
			context.Services.Scheduler.Enqueue(context, DoFilterSortingAndChunking, new SharedTag(items.ToList().AsReadOnly(), _writerCallbacks, _baseFileName, _outputDirectory));
		}

		private void DoFilterSortingAndChunking(WorkItemData<GlobalWriteContext, SharedTag> data)
		{
			SharedTag tag = data.Tag;
			List<TItem> list = BaseStreamManager<TItem, TStream>.FilterAndSort(data.Context, tag.Items, tag.Callbacks);
			ReadOnlyCollection<ReadOnlyCollection<TItem>> readOnlyCollection = BaseStreamManager<TItem, TStream>.Chunk(data.Context, list.AsReadOnly(), tag.Callbacks);
			for (int i = 0; i < readOnlyCollection.Count; i++)
			{
				ProcessChunk(data.Context, readOnlyCollection[i], BaseStreamManager<TItem, TStream>.GetChunkFilePath(tag.OutputDirectory, tag.BaseFileName, i), tag);
			}
		}

		protected abstract void ProcessChunk(GlobalWriteContext context, ReadOnlyCollection<TItem> chunk, NPath chunkFilePath, SharedTag sharedTag);
	}
}
