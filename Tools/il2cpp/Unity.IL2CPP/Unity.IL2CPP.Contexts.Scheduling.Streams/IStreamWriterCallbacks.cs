using System.Collections.Generic;
using System.Collections.ObjectModel;
using NiceIO;
using Unity.IL2CPP.Contexts.Services;

namespace Unity.IL2CPP.Contexts.Scheduling.Streams
{
	public interface IStreamWriterCallbacks<TItem, TStream>
	{
		TStream CreateWriter(SourceWritingContext context);

		IComparer<TItem> CreateComparer();

		void MergeAndFlushStreams(GlobalWriteContext context, ReadOnlyCollection<ResultData<TItem, TStream>> results, NPath filePath);

		void FlushStream(GlobalWriteContext context, TStream stream, NPath filePath);

		void WriteAndFlushStreams(GlobalWriteContext context, ReadOnlyCollection<TItem> items, NPath filePath);

		IEnumerable<TItem> FilterItemsForWriting(GlobalWriteContext context, ICollection<TItem> items);

		ReadOnlyCollection<ReadOnlyCollection<TItem>> Chunk(ReadOnlyCollection<TItem> items);

		void WriteItem(StreamWorkItemData<TItem, TStream> data);
	}
}
