using Unity.IL2CPP.Contexts.Scheduling;

namespace Unity.IL2CPP.Contexts.Services
{
	public delegate void ContinueAction<TWorkerContext, TTag>(WorkItemData<TWorkerContext, TTag> processor);
}
