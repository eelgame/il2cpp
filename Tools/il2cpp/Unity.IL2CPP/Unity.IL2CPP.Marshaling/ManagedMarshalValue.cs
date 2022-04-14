using System;
using Mono.Cecil;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.Marshaling
{
	public struct ManagedMarshalValue
	{
		private readonly string _objectVariableName;

		private readonly FieldReference _field;

		private readonly string _indexVariableName;

		private readonly ReadOnlyContext _context;

		public ManagedMarshalValue Dereferenced => new ManagedMarshalValue(_context, Emit.Dereference(_objectVariableName), _field, _indexVariableName);

		public ManagedMarshalValue(ReadOnlyContext context, string objectVariableName)
		{
			_context = context;
			_objectVariableName = objectVariableName;
			_field = null;
			_indexVariableName = null;
		}

		public ManagedMarshalValue(ReadOnlyContext context, string objectVariableName, FieldReference field)
		{
			_context = context;
			_objectVariableName = objectVariableName;
			_field = field;
			_indexVariableName = null;
		}

		public ManagedMarshalValue(ReadOnlyContext context, ManagedMarshalValue arrayValue, string indexVariableName)
		{
			_context = context;
			_objectVariableName = arrayValue._objectVariableName;
			_field = arrayValue._field;
			_indexVariableName = indexVariableName;
		}

		public ManagedMarshalValue(ReadOnlyContext context, string objectVariableName, FieldReference field, string indexVariableName)
		{
			_context = context;
			_objectVariableName = objectVariableName;
			_field = field;
			_indexVariableName = indexVariableName;
		}

		public string Load()
		{
			if (_indexVariableName != null)
			{
				string array = ((_field == null) ? _objectVariableName : $"{_objectVariableName}.{_context.Global.Services.Naming.ForFieldGetter(_field)}()");
				return Emit.LoadArrayElement(array, _indexVariableName, useArrayBoundsCheck: false);
			}
			if (_field != null)
			{
				return $"{_objectVariableName}.{_context.Global.Services.Naming.ForFieldGetter(_field)}()";
			}
			return _objectVariableName;
		}

		public string LoadAddress()
		{
			if (_indexVariableName != null)
			{
				throw new NotSupportedException();
			}
			if (_field != null)
			{
				return $"{_objectVariableName}.{_context.Global.Services.Naming.ForFieldAddressGetter(_field)}()";
			}
			return Emit.AddressOf(_objectVariableName);
		}

		public string Store(string value)
		{
			if (_indexVariableName != null)
			{
				string array = ((_field == null) ? _objectVariableName : $"{_objectVariableName}.{_context.Global.Services.Naming.ForFieldGetter(_field)}()");
				return $"{Emit.StoreArrayElement(array, _indexVariableName, value, useArrayBoundsCheck: false)};";
			}
			if (_field != null)
			{
				return $"{_objectVariableName}.{_context.Global.Services.Naming.ForFieldSetter(_field)}({value});";
			}
			return $"{_objectVariableName} = {value};";
		}

		public string Store(string format, params object[] args)
		{
			return Store(string.Format(format, args));
		}

		public string GetNiceName()
		{
			string text = _objectVariableName;
			if (_field != null)
			{
				text = text + "_" + _field.Name;
			}
			if (_indexVariableName != null)
			{
				text += "_item";
			}
			return _context.Global.Services.Naming.Clean(text.Replace("*", string.Empty));
		}

		public override string ToString()
		{
			throw new NotSupportedException();
		}
	}
}
