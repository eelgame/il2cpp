using Mono.Cecil;

namespace Unity.IL2CPP.AssemblyConversion.Steps.Base
{
	public abstract class PerAssemblyScheduledStepFuncWithGlobalPostProcessingFunc<TWorkerContext, TWorkerResult, TPostProcessResult> : ScheduledItemsStepFuncWithContinueFunc<TWorkerContext, AssemblyDefinition, TWorkerResult, TPostProcessResult>
	{
		protected override string ProfilerDetailsForItem(AssemblyDefinition workerItem)
		{
			return workerItem.Name.Name;
		}
	}
}
