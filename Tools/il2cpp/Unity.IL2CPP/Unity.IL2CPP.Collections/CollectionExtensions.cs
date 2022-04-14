using System.Collections.Generic;
using Mono.Cecil;

namespace Unity.IL2CPP.Collections
{
	public static class CollectionExtensions
	{
		public static GenericMethodCollection AsGenericMethodCollection(this IDictionary<MethodReference, uint> dictionary)
		{
			return new GenericMethodCollection(dictionary);
		}
	}
}
