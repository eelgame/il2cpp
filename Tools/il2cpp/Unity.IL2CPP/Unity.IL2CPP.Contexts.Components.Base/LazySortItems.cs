using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Unity.IL2CPP.Contexts.Components.Base
{
	public delegate ReadOnlyCollection<TItem> LazySortItems<TItem>(ICollection<TItem> items);
}
