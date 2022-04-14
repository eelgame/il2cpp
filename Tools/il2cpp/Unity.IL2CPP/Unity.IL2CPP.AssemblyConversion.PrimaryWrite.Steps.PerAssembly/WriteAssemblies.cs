using Mono.Cecil;
using Unity.IL2CPP.AssemblyConversion.Steps.Base;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.SourceWriters;

namespace Unity.IL2CPP.AssemblyConversion.PrimaryWrite.Steps.PerAssembly
{
	public class WriteAssemblies : PerAssemblyScheduledStepAction<GlobalWriteContext>
	{
		public const string StepName = "Write Assembly";

		protected override string Name => "Write Assembly";

		protected override bool Skip(GlobalWriteContext context)
		{
			return false;
		}

		protected override void ProcessItem(GlobalWriteContext context, AssemblyDefinition item)
		{
			TypeMethodsSourceWriter.EnqueueWrites(context.CreateAssemblyWritingContext(item));
		}
	}
}
