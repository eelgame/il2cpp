using System.Collections.Generic;
using System.Collections.ObjectModel;
using NiceIO;

namespace Unity.IL2CPP.Contexts.Scheduling.Streams
{
	public class SerialStreamManager<TItem, TStream> : BaseStreamManager<TItem, TStream> where TStream : IStream
	{
		public SerialStreamManager(NPath outputDirectory, NPath baseFileName, IStreamWriterCallbacks<TItem, TStream> writerCallbacks)
			: base(outputDirectory, baseFileName, writerCallbacks)
		{
		}

		public override void Write(GlobalWriteContext context, ICollection<TItem> items)
		{
			List<TItem> list = BaseStreamManager<TItem, TStream>.FilterAndSort(context, items, _writerCallbacks);
			ReadOnlyCollection<ReadOnlyCollection<TItem>> readOnlyCollection = BaseStreamManager<TItem, TStream>.Chunk(context, list.AsReadOnly(), _writerCallbacks);
			for (int i = 0; i < readOnlyCollection.Count; i++)
			{
				_writerCallbacks.WriteAndFlushStreams(context, readOnlyCollection[i], BaseStreamManager<TItem, TStream>.GetChunkFilePath(_outputDirectory, _baseFileName, i));
			}
		}
	}
}
