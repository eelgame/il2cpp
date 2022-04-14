using Unity.IL2CPP.Contexts.Scheduling;

namespace Unity.IL2CPP.Contexts.Services
{
	public delegate TResult WorkerFunc<TWorkerContext, TItem, TResult, TTag>(WorkItemData<TWorkerContext, TItem, TTag> data);
}
