using Mono.Cecil;

namespace Unity.IL2CPP.AssemblyConversion.Steps.Base
{
	public abstract class PerAssemblyScheduledStepFuncWithGlobalPostProcessing<TWorkerContext, TWorkerResult> : ScheduledItemsStepFuncWithContinueAction<TWorkerContext, AssemblyDefinition, TWorkerResult>
	{
		protected override string ProfilerDetailsForItem(AssemblyDefinition workerItem)
		{
			return workerItem.Name.Name;
		}
	}
}
