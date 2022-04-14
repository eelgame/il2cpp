using System;
using Unity.MiniProfiling;

namespace Unity.IL2CPP.AssemblyConversion.Steps.Base
{
	public abstract class BaseScheduledItemsStep<TWorkerContext, TWorkerItem> : ScheduledStep<TWorkerContext>
	{
		protected abstract string ProfilerDetailsForItem(TWorkerItem workerItem);

		protected IDisposable CreateProfilerSectionForProcessItem(TWorkerItem item)
		{
			return MiniProfiler.Section(Name, ProfilerDetailsForItem(item));
		}
	}
}
