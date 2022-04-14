using System.Collections.ObjectModel;
using Unity.IL2CPP.Metadata.RuntimeTypes;

namespace Unity.IL2CPP.Contexts.Results
{
	public interface ITypeCollectorResults
	{
		ReadOnlyCollection<IIl2CppRuntimeType> SortedItems { get; }

		int GetIndex(IIl2CppRuntimeType typeData);
	}
}
