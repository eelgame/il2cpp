using System.Collections.Generic;
using System.Collections.ObjectModel;
using Unity.IL2CPP.Metadata.RuntimeTypes;

namespace Unity.IL2CPP.Collections
{
	public class GenericInstancesCollection : ReadOnlyDictionary<IIl2CppRuntimeType[], uint>
	{
		public GenericInstancesCollection(IDictionary<IIl2CppRuntimeType[], uint> dictionary)
			: base(dictionary)
		{
		}
	}
}
