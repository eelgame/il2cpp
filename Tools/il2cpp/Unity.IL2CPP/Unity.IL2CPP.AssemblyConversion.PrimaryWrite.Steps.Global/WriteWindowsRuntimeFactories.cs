using Unity.IL2CPP.AssemblyConversion.Steps.Base;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.SourceWriters;

namespace Unity.IL2CPP.AssemblyConversion.PrimaryWrite.Steps.Global
{
	public class WriteWindowsRuntimeFactories : SimpleScheduledStep<GlobalWriteContext>
	{
		protected override string Name => "Write Windows Runtime Factories";

		protected override bool Skip(GlobalWriteContext context)
		{
			return context.Parameters.UsingTinyClassLibraries;
		}

		protected override void Worker(GlobalWriteContext context)
		{
			WindowsRuntimeFactoriesSourceWriter.EnqueueWrites(context.CreateSourceWritingContext());
		}
	}
}
