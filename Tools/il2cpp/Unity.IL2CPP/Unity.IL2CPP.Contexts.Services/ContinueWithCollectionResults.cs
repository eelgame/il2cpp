using System.Collections.ObjectModel;
using Unity.IL2CPP.Contexts.Scheduling;

namespace Unity.IL2CPP.Contexts.Services
{
	public delegate void ContinueWithCollectionResults<TWorkerContext, TItem, TResult, TTag>(WorkItemData<TWorkerContext, ReadOnlyCollection<ResultData<TItem, TResult>>, TTag> processor);
}
