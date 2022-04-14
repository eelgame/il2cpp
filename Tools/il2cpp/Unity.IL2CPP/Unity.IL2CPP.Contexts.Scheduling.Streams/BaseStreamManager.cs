using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using NiceIO;

namespace Unity.IL2CPP.Contexts.Scheduling.Streams
{
	public abstract class BaseStreamManager<TItem, TStream> where TStream : IStream
	{
		protected class SharedTag
		{
			public readonly ReadOnlyCollection<TItem> Items;

			public readonly IStreamWriterCallbacks<TItem, TStream> Callbacks;

			public readonly NPath BaseFileName;

			public readonly NPath OutputDirectory;

			public SharedTag(ReadOnlyCollection<TItem> items, IStreamWriterCallbacks<TItem, TStream> callbacks, NPath baseFileName, NPath outputDirectory)
			{
				Items = items;
				Callbacks = callbacks;
				BaseFileName = baseFileName;
				OutputDirectory = outputDirectory;
			}
		}

		protected readonly NPath _outputDirectory;

		protected readonly NPath _baseFileName;

		protected readonly IStreamWriterCallbacks<TItem, TStream> _writerCallbacks;

		public BaseStreamManager(NPath outputDirectory, NPath baseFileName, IStreamWriterCallbacks<TItem, TStream> writerCallbacks)
		{
			if (!baseFileName.IsRelative)
			{
				throw new ArgumentException("baseFileName must be a relative path but it was absolute");
			}
			_outputDirectory = outputDirectory;
			_baseFileName = baseFileName;
			_writerCallbacks = writerCallbacks;
		}

		public abstract void Write(GlobalWriteContext context, ICollection<TItem> items);

		protected static List<TItem> FilterAndSort(GlobalWriteContext context, ICollection<TItem> items, IStreamWriterCallbacks<TItem, TStream> callbacks)
		{
			List<TItem> list = callbacks.FilterItemsForWriting(context, items).ToList();
			list.Sort(callbacks.CreateComparer());
			return list;
		}

		protected static string GetChunkFilePath(NPath outputDirectory, NPath baseFileName, int index)
		{
			if (index == 0)
			{
				return outputDirectory.Combine(baseFileName);
			}
			return outputDirectory.Combine($"{baseFileName.FileNameWithoutExtension}{index}{baseFileName.ExtensionWithDot}");
		}

		protected static ReadOnlyCollection<ReadOnlyCollection<TItem>> Chunk(GlobalWriteContext context, ReadOnlyCollection<TItem> items, IStreamWriterCallbacks<TItem, TStream> callbacks)
		{
			return callbacks.Chunk(items);
		}

		protected static TStream GetAvailableStream(SourceWritingContext context, IStreamWriterCallbacks<TItem, TStream> callbacks)
		{
			return callbacks.CreateWriter(context);
		}
	}
}
