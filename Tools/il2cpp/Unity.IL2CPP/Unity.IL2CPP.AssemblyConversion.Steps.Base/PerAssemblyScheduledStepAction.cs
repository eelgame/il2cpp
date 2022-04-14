using Mono.Cecil;

namespace Unity.IL2CPP.AssemblyConversion.Steps.Base
{
	public abstract class PerAssemblyScheduledStepAction<TWorkerContext> : ScheduledItemsStepAction<TWorkerContext, AssemblyDefinition>
	{
		protected override string ProfilerDetailsForItem(AssemblyDefinition workerItem)
		{
			return workerItem.Name.Name;
		}
	}
}
