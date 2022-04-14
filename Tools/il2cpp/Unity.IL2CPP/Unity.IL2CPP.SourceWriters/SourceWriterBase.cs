using System.Collections.Generic;
using System.Collections.ObjectModel;
using NiceIO;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Scheduling.Streams;
using Unity.IL2CPP.Contexts.Services;
using Unity.MiniProfiling;

namespace Unity.IL2CPP.SourceWriters
{
	public abstract class SourceWriterBase<TItem> : IStreamWriterCallbacks<TItem, IGeneratedMethodCodeWriter>
	{
		public const int DefaultMethodBodyChunkSize = 26000;

		protected abstract string Name { get; }

		public IGeneratedMethodCodeWriter CreateWriter(SourceWritingContext context)
		{
			return new InMemoryGeneratedMethodCodeWriter(context);
		}

		public abstract IComparer<TItem> CreateComparer();

		public void MergeAndFlushStreams(GlobalWriteContext context, ReadOnlyCollection<ResultData<TItem, IGeneratedMethodCodeWriter>> results, NPath filePath)
		{
			string text;
			using (IGeneratedMethodCodeWriter generatedMethodCodeWriter = context.CreateProfiledSourceWriter(filePath))
			{
				WriteHeader(generatedMethodCodeWriter);
				foreach (ResultData<TItem, IGeneratedMethodCodeWriter> result in results)
				{
					generatedMethodCodeWriter.Write(result.Result);
				}
				WriteFooter(generatedMethodCodeWriter);
				WriteEnd(generatedMethodCodeWriter);
				text = generatedMethodCodeWriter.FileName;
			}
			context.Collectors.Symbols.CollectLineNumberInformation(context.GetReadOnlyContext(), text);
		}

		public void FlushStream(GlobalWriteContext context, IGeneratedMethodCodeWriter stream, NPath filePath)
		{
			string text;
			using (IGeneratedMethodCodeWriter generatedMethodCodeWriter = context.CreateProfiledSourceWriter(filePath))
			{
				WriteHeader(generatedMethodCodeWriter);
				generatedMethodCodeWriter.Write(stream);
				WriteFooter(generatedMethodCodeWriter);
				WriteEnd(generatedMethodCodeWriter);
				text = generatedMethodCodeWriter.FileName;
			}
			context.Collectors.Symbols.CollectLineNumberInformation(context.GetReadOnlyContext(), text);
		}

		public void WriteAndFlushStreams(GlobalWriteContext context, ReadOnlyCollection<TItem> items, NPath filePath)
		{
			SourceWritingContext context2 = context.CreateSourceWritingContext();
			string text;
			using (IGeneratedMethodCodeWriter generatedMethodCodeWriter = context2.CreateProfiledManagedSourceWriter(filePath))
			{
				WriteHeader(generatedMethodCodeWriter);
				foreach (TItem item in items)
				{
					WriteItem(new StreamWorkItemData<TItem, IGeneratedMethodCodeWriter>(context2, item, generatedMethodCodeWriter, filePath));
				}
				WriteFooter(generatedMethodCodeWriter);
				WriteEnd(generatedMethodCodeWriter);
				text = generatedMethodCodeWriter.FileName;
			}
			context.Collectors.Symbols.CollectLineNumberInformation(context.GetReadOnlyContext(), text);
		}

		public abstract IEnumerable<TItem> FilterItemsForWriting(GlobalWriteContext context, ICollection<TItem> items);

		public abstract ReadOnlyCollection<ReadOnlyCollection<TItem>> Chunk(ReadOnlyCollection<TItem> items);

		public void WriteItem(StreamWorkItemData<TItem, IGeneratedMethodCodeWriter> data)
		{
			using (MiniProfiler.Section(Name, ProfilerSectionDetailsForItem(data.Item)))
			{
				WriteItem(data.Context, data.Stream, data.Item, data.FilePath);
			}
		}

		protected abstract string ProfilerSectionDetailsForItem(TItem item);

		protected abstract void WriteItem(SourceWritingContext context, IGeneratedMethodCodeWriter writer, TItem item, NPath filePath);

		protected abstract void WriteHeader(IGeneratedMethodCodeWriter writer);

		protected abstract void WriteFooter(IGeneratedMethodCodeWriter writer);

		protected abstract void WriteEnd(IGeneratedMethodCodeWriter writer);
	}
}
