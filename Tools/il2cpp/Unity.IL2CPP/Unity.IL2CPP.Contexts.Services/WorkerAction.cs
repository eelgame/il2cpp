using Unity.IL2CPP.Contexts.Scheduling;

namespace Unity.IL2CPP.Contexts.Services
{
	public delegate void WorkerAction<TWorkerContext, TItem, TTag>(WorkItemData<TWorkerContext, TItem, TTag> data);
}
