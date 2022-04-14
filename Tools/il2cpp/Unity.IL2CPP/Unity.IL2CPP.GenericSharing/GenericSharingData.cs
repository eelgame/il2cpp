using System.Collections.ObjectModel;
using Unity.IL2CPP.Common;

namespace Unity.IL2CPP.GenericSharing
{
	public class GenericSharingData
	{
		private readonly ReadOnlyCollection<RuntimeGenericData> _rgctxs;

		public static readonly GenericSharingData Empty = new GenericSharingData(new RuntimeGenericData[0].AsReadOnly());

		public ReadOnlyCollection<RuntimeGenericData> RuntimeGenericDatas => _rgctxs;

		public GenericSharingData(ReadOnlyCollection<RuntimeGenericData> rgctxs)
		{
			_rgctxs = rgctxs;
		}
	}
}
