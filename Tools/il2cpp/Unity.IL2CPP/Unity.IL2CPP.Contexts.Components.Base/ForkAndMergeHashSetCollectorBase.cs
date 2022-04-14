using System.Collections.Generic;

namespace Unity.IL2CPP.Contexts.Components.Base
{
	public abstract class ForkAndMergeHashSetCollectorBase<TItem, TComplete, TWrite, TFull> : ForkAndMergeCollectionCollectorBase<TItem, TComplete, TWrite, TFull> where TFull : ForkAndMergeHashSetCollectorBase<TItem, TComplete, TWrite, TFull>, TWrite
	{
		protected ForkAndMergeHashSetCollectorBase()
			: this((IEqualityComparer<TItem>)null)
		{
		}

		protected ForkAndMergeHashSetCollectorBase(IEqualityComparer<TItem> comparer)
			: base((ICollection<TItem>)((comparer == null) ? new HashSet<TItem>() : new HashSet<TItem>(comparer)))
		{
		}
	}
}
