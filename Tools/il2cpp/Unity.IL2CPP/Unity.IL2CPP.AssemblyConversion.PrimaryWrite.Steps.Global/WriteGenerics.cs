using Unity.IL2CPP.AssemblyConversion.Steps.Base;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.SourceWriters;
using Unity.MiniProfiling;

namespace Unity.IL2CPP.AssemblyConversion.PrimaryWrite.Steps.Global
{
	public class WriteGenerics : CustomScheduledStep<GlobalWriteContext>
	{
		private readonly bool _includeGenerics;

		protected override string Name => "Write Generics";

		public WriteGenerics(bool includeGenerics)
		{
			_includeGenerics = includeGenerics;
		}

		protected override bool Skip(GlobalWriteContext context)
		{
			return !_includeGenerics;
		}

		protected override void DoScheduling(GlobalWriteContext context)
		{
			context.Services.Scheduler.Enqueue(context, delegate(GlobalWriteContext workerContext)
			{
				using (MiniProfiler.Section("GenericInstanceMethods"))
				{
					GenericMethodSourceWriter.EnqueueWrites(workerContext.CreateSourceWritingContext(), workerContext.Results.PrimaryCollection.Generics.Methods);
				}
			});
			context.Services.Scheduler.Enqueue(context, delegate(GlobalWriteContext workerContext)
			{
				using (MiniProfiler.Section("GenericInstanceTypes"))
				{
					GenericInstanceTypeSourceWriter.EnqueueWrites(workerContext.CreateSourceWritingContext(), "Generics", workerContext.Results.PrimaryCollection.Generics.Types);
				}
			});
			context.Services.Scheduler.Enqueue(context, delegate(GlobalWriteContext workerContext)
			{
				using (MiniProfiler.Section("GenericComDefinitions"))
				{
					GenericComDefinitionSourceWriter.EnqueueWrites(workerContext.CreateSourceWritingContext(), workerContext.Results.PrimaryCollection.Generics.TypeDeclarations);
				}
			});
		}
	}
}
