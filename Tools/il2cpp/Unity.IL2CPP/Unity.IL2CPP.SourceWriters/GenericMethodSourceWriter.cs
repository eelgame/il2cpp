using System.Collections.Generic;
using System.Collections.ObjectModel;
using Mono.Cecil;
using NiceIO;
using Unity.Cecil.Awesome.Ordering;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Scheduling.Streams;

namespace Unity.IL2CPP.SourceWriters
{
	public class GenericMethodSourceWriter : SourceWriterBase<GenericInstanceMethod>
	{
		private const string BaseFileName = "GenericMethods";

		protected override string Name => "WriteGenericMethodDefinition";

		private GenericMethodSourceWriter()
		{
		}

		public static void EnqueueWrites(SourceWritingContext context, ICollection<GenericInstanceMethod> items)
		{
			ScheduledStreamFactory.Create(context, "GenericMethods.cpp".ToNPath(), new GenericMethodSourceWriter()).Write(context.Global, items);
		}

		public override IComparer<GenericInstanceMethod> CreateComparer()
		{
			return new MethodOrderingComparer();
		}

		public override IEnumerable<GenericInstanceMethod> FilterItemsForWriting(GlobalWriteContext context, ICollection<GenericInstanceMethod> items)
		{
			return items;
		}

		public override ReadOnlyCollection<ReadOnlyCollection<GenericInstanceMethod>> Chunk(ReadOnlyCollection<GenericInstanceMethod> items)
		{
			return items.ChunkByCodeSize(26000);
		}

		protected override string ProfilerSectionDetailsForItem(GenericInstanceMethod item)
		{
			return item.Name;
		}

		protected override void WriteItem(SourceWritingContext context, IGeneratedMethodCodeWriter writer, GenericInstanceMethod item, NPath filePath)
		{
			SourceWriter.WriteGenericMethodDefinition(context, writer, item);
		}

		protected override void WriteHeader(IGeneratedMethodCodeWriter writer)
		{
		}

		protected override void WriteFooter(IGeneratedMethodCodeWriter writer)
		{
		}

		protected override void WriteEnd(IGeneratedMethodCodeWriter writer)
		{
			MethodWriter.WriteInlineMethodDefinitions(writer.Context, writer.FileName.FileNameWithoutExtension, writer);
		}
	}
}
