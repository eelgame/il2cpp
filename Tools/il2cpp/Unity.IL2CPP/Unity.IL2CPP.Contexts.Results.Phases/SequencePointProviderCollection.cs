using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Mono.Cecil;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Debugger;

namespace Unity.IL2CPP.Contexts.Results.Phases
{
	public class SequencePointProviderCollection
	{
		private class NotCollected : ISequencePointProvider
		{
			private readonly AssemblyDefinition _assembly;

			private string ExceptionMessage => $"Sequence Points were never collected for assembly : {_assembly.Name}";

			public NotCollected(AssemblyDefinition assembly)
			{
				_assembly = assembly;
			}

			public SequencePointInfo GetSequencePointAt(MethodDefinition method, int ilOffset, SequencePointKind kind)
			{
				throw new NotSupportedException(ExceptionMessage);
			}

			public bool TryGetSequencePointAt(MethodDefinition method, int ilOffset, SequencePointKind kind, out SequencePointInfo info)
			{
				throw new NotSupportedException(ExceptionMessage);
			}

			public int GetSeqPointIndex(SequencePointInfo seqPoint)
			{
				throw new NotSupportedException(ExceptionMessage);
			}

			public bool MethodHasSequencePoints(MethodDefinition method)
			{
				throw new NotSupportedException(ExceptionMessage);
			}

			public bool MethodHasPausePointAtOffset(MethodDefinition method, int offset)
			{
				throw new NotSupportedException(ExceptionMessage);
			}
		}

		private readonly ReadOnlyDictionary<AssemblyDefinition, ISequencePointProvider> _providers;

		public static SequencePointProviderCollection Empty => new SequencePointProviderCollection(new Dictionary<AssemblyDefinition, ISequencePointProvider>().AsReadOnly());

		public SequencePointProviderCollection(ReadOnlyDictionary<AssemblyDefinition, ISequencePointProvider> providers)
		{
			_providers = providers;
		}

		public ISequencePointProvider GetProvider(AssemblyDefinition assembly)
		{
			if (_providers.TryGetValue(assembly, out var value))
			{
				return value;
			}
			return new NotCollected(assembly);
		}

		public static SequencePointProviderCollection Merge(IEnumerable<SequencePointProviderCollection> results)
		{
			Dictionary<AssemblyDefinition, ISequencePointProvider> dictionary = new Dictionary<AssemblyDefinition, ISequencePointProvider>();
			foreach (SequencePointProviderCollection result in results)
			{
				foreach (KeyValuePair<AssemblyDefinition, ISequencePointProvider> provider in result._providers)
				{
					dictionary.Add(provider.Key, provider.Value);
				}
			}
			return new SequencePointProviderCollection(dictionary.AsReadOnly());
		}
	}
}
