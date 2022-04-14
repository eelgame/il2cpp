using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts;

namespace Unity.IL2CPP.Metadata
{
	public class FieldReferenceCollector : IFieldReferenceCollector, IFieldReferenceCollection, IDisposable
	{
		private readonly Dictionary<Il2CppRuntimeFieldReference, uint> _fields = new Dictionary<Il2CppRuntimeFieldReference, uint>();

		private readonly SourceWritingContext _context;

		public ReadOnlyDictionary<Il2CppRuntimeFieldReference, uint> Fields => _fields.AsReadOnly();

		public FieldReferenceCollector(SourceWritingContext context)
		{
			_context = context;
		}

		public uint GetOrCreateIndex(Il2CppRuntimeFieldReference il2CppRuntimeField)
		{
			if (_fields.TryGetValue(il2CppRuntimeField, out var value))
			{
				return value;
			}
			value = (uint)_fields.Count;
			_fields.Add(il2CppRuntimeField, value);
			return value;
		}

		public void Dispose()
		{
			_fields.Clear();
		}
	}
}
