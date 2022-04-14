using System.Collections.Generic;
using System.Collections.ObjectModel;
using Mono.Cecil;
using NiceIO;
using Unity.Cecil.Awesome.Comparers;
using Unity.Cecil.Awesome.Ordering;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Scheduling.Streams;
using Unity.IL2CPP.Metadata.RuntimeTypes;
using Unity.IL2CPP.WindowsRuntime;
using Unity.MiniProfiling;

namespace Unity.IL2CPP.SourceWriters
{
	public class ProjectedInterfacesByComCallableWrappersSourceWriter : SourceWriterBase<TypeReference>
	{
		protected override string Name => "WriteProjectedComCallableWrapperMethods";

		private ProjectedInterfacesByComCallableWrappersSourceWriter()
		{
		}

		public static void EnqueueWrites(SourceWritingContext context, string fileName, ICollection<TypeReference> items)
		{
			ScheduledStreamFactory.Create(context, (fileName + ".cpp").ToNPath(), new ProjectedInterfacesByComCallableWrappersSourceWriter()).Write(context.Global, items);
		}

		public static void EnqueueWrites(SourceWritingContext context)
		{
			ReadOnlyCollection<IIl2CppRuntimeType> cCWMarshalingFunctions = context.Global.PrimaryCollectionResults.CCWMarshalingFunctions;
			HashSet<TypeReference> hashSet;
			using (MiniProfiler.Section("Collect implemented projected interfaces by COM Callable Wrappers"))
			{
				hashSet = CollectImplementedProjectedInterfacesByComCallableWrappersOf(context, cCWMarshalingFunctions);
			}
			if (hashSet.Count != 0)
			{
				EnqueueWrites(context, "Il2CppPCCWMethods", hashSet);
			}
		}

		private static HashSet<TypeReference> CollectImplementedProjectedInterfacesByComCallableWrappersOf(ReadOnlyContext context, ReadOnlyCollection<IIl2CppRuntimeType> typesWithComCallableWrappers)
		{
			HashSet<TypeReference> hashSet = new HashSet<TypeReference>(new TypeReferenceEqualityComparer());
			foreach (IIl2CppRuntimeType typesWithComCallableWrapper in typesWithComCallableWrappers)
			{
				foreach (TypeReference item in typesWithComCallableWrapper.Type.GetInterfacesImplementedByComCallableWrapper(context))
				{
					if (context.Global.Services.WindowsRuntime.ProjectToCLR(item) != item)
					{
						hashSet.Add(item);
					}
				}
			}
			return hashSet;
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
			return new List<ReadOnlyCollection<TypeReference>> { items }.AsReadOnly();
		}

		protected override string ProfilerSectionDetailsForItem(TypeReference item)
		{
			return item.FullName;
		}

		protected override void WriteItem(SourceWritingContext context, IGeneratedMethodCodeWriter writer, TypeReference item, NPath filePath)
		{
			ProjectedComCallableWrapperMethodWriterDriver.WriteFor(context, writer, item);
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
