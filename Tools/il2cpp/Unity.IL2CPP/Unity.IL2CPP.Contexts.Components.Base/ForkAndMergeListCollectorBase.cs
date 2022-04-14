using System.Collections.Generic;

namespace Unity.IL2CPP.Contexts.Components.Base
{
	public abstract class ForkAndMergeListCollectorBase<TItem, TComplete, TWrite, TFull> : ForkAndMergeCollectionCollectorBase<TItem, TComplete, TWrite, TFull> where TFull : ForkAndMergeListCollectorBase<TItem, TComplete, TWrite, TFull>, TWrite
	{
		protected ForkAndMergeListCollectorBase()
			: base((ICollection<TItem>)new List<TItem>())
		{
		}
	}
}
