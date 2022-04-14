using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Mono.Cecil;
using NiceIO;
using Unity.Cecil.Awesome.Ordering;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Com;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Scheduling.Streams;
using Unity.IL2CPP.Metadata.RuntimeTypes;
using Unity.IL2CPP.WindowsRuntime;

namespace Unity.IL2CPP.SourceWriters
{
	public class ComCallableWrappersSourceWriter : SourceWriterBase<TypeReference>
	{
		protected override string Name => "WriteComCallableWrapperMethods";

		private ComCallableWrappersSourceWriter()
		{
		}

		public static void EnqueueWrites(SourceWritingContext context, string fileName, ICollection<TypeReference> items)
		{
			ScheduledStreamFactory.Create(context, (fileName + ".cpp").ToNPath(), new ComCallableWrappersSourceWriter()).Write(context.Global, items);
		}

		public static void EnqueueWrites(SourceWritingContext context)
		{
			ReadOnlyCollection<IIl2CppRuntimeType> cCWMarshalingFunctions = context.Global.PrimaryCollectionResults.CCWMarshalingFunctions;
			if (cCWMarshalingFunctions.Count != 0)
			{
				EnqueueWrites(context, "Il2CppCCWs", cCWMarshalingFunctions.Select((IIl2CppRuntimeType t) => t.Type).ToList());
			}
		}

		public override IComparer<TypeReference> CreateComparer()
		{
			return new TypeOrderingComparer();
		}

		public override IEnumerable<TypeReference> FilterItemsForWriting(GlobalWriteContext context, ICollection<TypeReference> items)
		{
			return items;
		}

		public override ReadOnlyCollection<ReadOnlyCollection<TypeReference>> Chunk(ReadOnlyCollection<TypeReference> items)
		{
			return items.ChunkByCodeSize(26000);
		}

		protected override string ProfilerSectionDetailsForItem(TypeReference item)
		{
			return item.FullName;
		}

		protected override void WriteItem(SourceWritingContext context, IGeneratedMethodCodeWriter writer, TypeReference item, NPath filePath)
		{
			ICCWWriter ccwWriter = GetCCWWriterForType(context, item);
			ccwWriter.Write(writer);
			writer.WriteLine();
			string text = context.Global.Services.Naming.ForCreateComCallableWrapperFunction(item);
			string methodSignature = "IL2CPP_EXTERN_C Il2CppIUnknown* " + text + "(RuntimeObject* obj)";
			writer.WriteMethodWithMetadataInitialization(methodSignature, text, delegate(IGeneratedMethodCodeWriter bodyWriter, IRuntimeMetadataAccess metadataAccess)
			{
				ccwWriter.WriteCreateComCallableWrapperFunctionBody(bodyWriter, metadataAccess);
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

		private static ICCWWriter GetCCWWriterForType(SourceWritingContext context, TypeReference type)
		{
			if (type.IsArray)
			{
				return new CCWWriter(context, type);
			}
			TypeDefinition typeDefinition = type.Resolve();
			if (typeDefinition.IsDelegate())
			{
				return new DelegateCCWWriter(context, type);
			}
			TypeDefinition typeDefinition2 = context.Global.Services.WindowsRuntime.ProjectToWindowsRuntime(typeDefinition);
			if (typeDefinition2 != typeDefinition && !typeDefinition2.IsInterface && !typeDefinition2.IsValueType)
			{
				return new ProjectedClassCCWWriter(type);
			}
			return new CCWWriter(context, type);
		}
	}
}
