using System.Collections.Generic;
using System.Collections.ObjectModel;
using Mono.Cecil;

namespace Unity.IL2CPP.Collections
{
	public class GenericMethodCollection : ReadOnlyDictionary<MethodReference, uint>
	{
		public GenericMethodCollection(IDictionary<MethodReference, uint> dictionary)
			: base(dictionary)
		{
		}
	}
}
