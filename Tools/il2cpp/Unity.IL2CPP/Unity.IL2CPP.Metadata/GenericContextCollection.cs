using System.Collections.ObjectModel;
using Mono.Cecil;

namespace Unity.IL2CPP.Metadata
{
	public class GenericContextCollection
	{
		private readonly ReadOnlyCollection<RGCTXEntry> _rgctxEntries;

		private readonly ReadOnlyDictionary<IGenericParameterProvider, int> _rgctxEntriesStart;

		private readonly ReadOnlyDictionary<IGenericParameterProvider, int> _rgctxEntriesCount;

		public GenericContextCollection(ReadOnlyCollection<RGCTXEntry> rgctxEntries, ReadOnlyDictionary<IGenericParameterProvider, int> rgctxEntriesStart, ReadOnlyDictionary<IGenericParameterProvider, int> rgctxEntriesCount)
		{
			_rgctxEntries = rgctxEntries;
			_rgctxEntriesStart = rgctxEntriesStart;
			_rgctxEntriesCount = rgctxEntriesCount;
		}

		public ReadOnlyCollection<RGCTXEntry> GetRGCTXEntries()
		{
			return _rgctxEntries;
		}

		public int GetRGCTXEntriesStartIndex(IGenericParameterProvider provider)
		{
			if (_rgctxEntriesStart.TryGetValue(provider, out var value))
			{
				return value;
			}
			return -1;
		}

		public int GetRGCTXEntriesCount(IGenericParameterProvider provider)
		{
			if (_rgctxEntriesCount.TryGetValue(provider, out var value))
			{
				return value;
			}
			return 0;
		}
	}
}
