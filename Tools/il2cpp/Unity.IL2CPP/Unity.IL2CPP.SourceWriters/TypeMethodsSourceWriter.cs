using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using NiceIO;
using Unity.Cecil.Awesome.Ordering;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Scheduling.Streams;

namespace Unity.IL2CPP.SourceWriters
{
	public class TypeMethodsSourceWriter : SourceWriterBase<TypeDefinition>
	{
		protected override string Name => "WriteMethods";

		private TypeMethodsSourceWriter()
		{
		}

		public static void EnqueueWrites(SourceWritingContext context, AssemblyDefinition assembly, ICollection<TypeDefinition> items)
		{
			ScheduledStreamFactory.Create(context, context.Global.Services.PathFactory.GetFileNameForAssembly(assembly, ".cpp"), new TypeMethodsSourceWriter()).Write(context.Global, items);
		}

		public static void EnqueueWrites(AssemblyWriteContext context)
		{
			AssemblyDefinition assemblyDefinition = context.AssemblyDefinition;
			EnqueueWrites(context.SourceWritingContext, assemblyDefinition, assemblyDefinition.MainModule.GetAllTypes().ToArray());
		}

		public override IComparer<TypeDefinition> CreateComparer()
		{
			return new TypeOrderingComparer();
		}

		public override IEnumerable<TypeDefinition> FilterItemsForWriting(GlobalWriteContext context, ICollection<TypeDefinition> items)
		{
			return items.Where((TypeDefinition m) => MethodWriter.TypeMethodsCanBeDirectlyCalled(context.GetReadOnlyContext(), m));
		}

		public override ReadOnlyCollection<ReadOnlyCollection<TypeDefinition>> Chunk(ReadOnlyCollection<TypeDefinition> items)
		{
			return items.ChunkByCodeSize(26000);
		}

		protected override string ProfilerSectionDetailsForItem(TypeDefinition item)
		{
			return item.FullName;
		}

		protected override void WriteItem(SourceWritingContext context, IGeneratedMethodCodeWriter writer, TypeDefinition item, NPath filePath)
		{
			SourceWriter.WriteType(context, writer, item, filePath, writeMarshalingDefinitions: true, addToMethodCollection: true);
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
