using Unity.IL2CPP.AssemblyConversion.Steps.Base;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.SourceWriters;

namespace Unity.IL2CPP.AssemblyConversion.PrimaryWrite.Steps.Global
{
	public class WriteProjectedInterfacesByComCallableWrappers : SimpleScheduledStep<GlobalWriteContext>
	{
		protected override string Name => "Write Projected Interfaces by CCW";

		protected override bool Skip(GlobalWriteContext context)
		{
			return context.PrimaryCollectionResults.CCWMarshalingFunctions.Count == 0;
		}

		protected override void Worker(GlobalWriteContext context)
		{
			ProjectedInterfacesByComCallableWrappersSourceWriter.EnqueueWrites(context.CreateSourceWritingContext());
		}
	}
}
