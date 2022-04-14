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
	public class GenericComDefinitionSourceWriter : SourceWriterBase<GenericInstanceType>
	{
		private const string BaseFileName = "Il2CppGenericComDefinitions";

		protected override string Name => "WriteGenericComDefinition";

		private GenericComDefinitionSourceWriter()
		{
		}

		public static void EnqueueWrites(SourceWritingContext context, ICollection<GenericInstanceType> items)
		{
			ScheduledStreamFactory.Create(context, "Il2CppGenericComDefinitions.cpp".ToNPath(), new GenericComDefinitionSourceWriter()).Write(context.Global, items);
		}

		public override IComparer<GenericInstanceType> CreateComparer()
		{
			return new TypeOrderingComparer();
		}

		public override IEnumerable<GenericInstanceType> FilterItemsForWriting(GlobalWriteContext context, ICollection<GenericInstanceType> items)
		{
			return items;
		}

		public override ReadOnlyCollection<ReadOnlyCollection<GenericInstanceType>> Chunk(ReadOnlyCollection<GenericInstanceType> items)
		{
			return items.ChunkByCodeSize(26000);
		}

		protected override string ProfilerSectionDetailsForItem(GenericInstanceType item)
		{
			return item.FullName;
		}

		protected override void WriteItem(SourceWritingContext context, IGeneratedMethodCodeWriter writer, GenericInstanceType item, NPath filePath)
		{
			SourceWriter.WriteMarshalingDefinitions(context, writer, item);
		}

		protected override void WriteHeader(IGeneratedMethodCodeWriter writer)
		{
		}

		protected override void WriteFooter(IGeneratedMethodCodeWriter writer)
		{
		}

		protected override void WriteEnd(IGeneratedMethodCodeWriter writer)
		{
		}
	}
}
