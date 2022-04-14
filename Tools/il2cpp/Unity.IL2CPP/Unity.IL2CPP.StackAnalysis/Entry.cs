using System.Collections.Generic;
using Mono.Cecil;
using Unity.Cecil.Awesome.Comparers;

namespace Unity.IL2CPP.StackAnalysis
{
	public class Entry
	{
		private readonly HashSet<TypeReference> _types = new HashSet<TypeReference>(new TypeReferenceEqualityComparer());

		public bool NullValue { get; internal set; }

		public HashSet<TypeReference> Types => _types;

		public Entry Clone()
		{
			Entry entry = new Entry
			{
				NullValue = NullValue
			};
			foreach (TypeReference type in _types)
			{
				entry.Types.Add(type);
			}
			return entry;
		}

		public static Entry For(TypeReference typeReference)
		{
			return new Entry
			{
				Types = { typeReference }
			};
		}

		public static Entry ForNull(TypeReference typeReference)
		{
			return new Entry
			{
				NullValue = true,
				Types = { typeReference }
			};
		}
	}
}
