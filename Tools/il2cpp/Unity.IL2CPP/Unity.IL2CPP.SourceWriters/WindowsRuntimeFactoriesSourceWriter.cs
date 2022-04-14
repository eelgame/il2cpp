using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Mono.Cecil;
using NiceIO;
using Unity.Cecil.Awesome.Ordering;
using Unity.IL2CPP.AssemblyConversion.PrimaryCollection.Results;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Scheduling.Streams;
using Unity.IL2CPP.WindowsRuntime;

namespace Unity.IL2CPP.SourceWriters
{
	public class WindowsRuntimeFactoriesSourceWriter : SourceWriterBase<TypeDefinition>
	{
		protected override string Name => "Write Windows Runtime Factory";

		private WindowsRuntimeFactoriesSourceWriter()
		{
		}

		private static void EnqueueWrites(SourceWritingContext context, string fileName, ICollection<TypeDefinition> items)
		{
			ScheduledStreamFactory.Create(context, (fileName + ".cpp").ToNPath(), new WindowsRuntimeFactoriesSourceWriter()).Write(context.Global, items);
		}

		public static void EnqueueWrites(SourceWritingContext context)
		{
			List<WindowsRuntimeFactoryData> list = DictionaryExtensions.ItemsSortedByKey(context.Global.PrimaryCollectionResults.WindowsRuntimeData).SelectMany((KeyValuePair<AssemblyDefinition, CollectedWindowsRuntimeData> pair) => pair.Value.RuntimeFactories).ToList();
			if (list.Count != 0)
			{
				EnqueueWrites(context, "Il2CppWindowsRuntimeFactories", list.Select((WindowsRuntimeFactoryData t) => t.TypeDefinition).ToList());
			}
		}

		public override IComparer<TypeDefinition> CreateComparer()
		{
			return new TypeOrderingComparer();
		}

		public override IEnumerable<TypeDefinition> FilterItemsForWriting(GlobalWriteContext context, ICollection<TypeDefinition> items)
		{
			return items;
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
			WindowsRuntimeFactoryWriter factoryWriter = new WindowsRuntimeFactoryWriter(context, item);
			factoryWriter.Write(writer);
			string text = context.Global.Services.Naming.ForCreateWindowsRuntimeFactoryFunction(item);
			string methodSignature = "Il2CppIActivationFactory* " + text + "()";
			writer.WriteMethodWithMetadataInitialization(methodSignature, text, delegate(IGeneratedMethodCodeWriter bodyWriter, IRuntimeMetadataAccess metadataAccess)
			{
				factoryWriter.WriteCreateComCallableWrapperFunctionBody(bodyWriter, metadataAccess);
			}, text, null);
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
