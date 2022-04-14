using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Unity.Cecil.Awesome.Comparers;

namespace Unity.IL2CPP.StackAnalysis
{
	public class StackState
	{
		private Stack<Entry> _entries = new Stack<Entry>();

		public Stack<Entry> Entries => _entries;

		public bool IsEmpty => _entries.Count == 0;

		public void Merge(StackState other)
		{
			List<Entry> list = new List<Entry>(_entries);
			List<Entry> list2 = new List<Entry>(other.Entries);
			while (list.Count < list2.Count)
			{
				list.Add(new Entry());
			}
			for (int i = 0; i < list2.Count; i++)
			{
				Entry entry = list2[i];
				Entry entry2 = list[i];
				if (entry2.Types.Count == 1 && entry.Types.Count == 1)
				{
					TypeReference a = entry2.Types.First();
					TypeReference typeReference = entry.Types.First();
					if (!TypeReferenceEqualityComparer.AreEqual(a, typeReference))
					{
						if (entry2.NullValue)
						{
							entry2.NullValue = entry.NullValue;
							entry2.Types.Clear();
							entry2.Types.Add(typeReference);
							continue;
						}
						if (entry.NullValue)
						{
							continue;
						}
					}
				}
				entry2.NullValue |= entry.NullValue;
				foreach (TypeReference type in entry.Types)
				{
					entry2.Types.Add(type);
				}
			}
			list.Reverse();
			_entries = new Stack<Entry>(list);
		}

		public StackState Clone()
		{
			StackState stackState = new StackState();
			foreach (Entry item in _entries.Reverse())
			{
				stackState.Entries.Push(item.Clone());
			}
			return stackState;
		}
	}
}
