using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Unity.IL2CPP.StringLiterals
{
	public class StringLiteralCollection : IStringLiteralCollection
	{
		private readonly Dictionary<StringMetadataToken, int> _stringLiterals;

		private bool _complete;

		public StringLiteralCollection()
		{
			_stringLiterals = new Dictionary<StringMetadataToken, int>(new StringMetadataTokenComparer());
		}

		public IStringLiteralCollection Complete()
		{
			_complete = true;
			return this;
		}

		public int Add(StringMetadataToken stringMetadataToken)
		{
			AssertNotComplete();
			if (_stringLiterals.TryGetValue(stringMetadataToken, out var value))
			{
				return value;
			}
			value = _stringLiterals.Count;
			_stringLiterals.Add(stringMetadataToken, value);
			return value;
		}

		public ReadOnlyCollection<string> GetStringLiterals()
		{
			AssertComplete();
			return _stringLiterals.KeysSortedByValue((StringMetadataToken key) => key.Literal);
		}

		public ReadOnlyCollection<StringMetadataToken> GetStringMetadataTokens()
		{
			AssertComplete();
			return _stringLiterals.KeysSortedByValue();
		}

		public int GetIndex(StringMetadataToken stringMetadataToken)
		{
			AssertComplete();
			return _stringLiterals[stringMetadataToken];
		}

		private void AssertComplete()
		{
			if (!_complete)
			{
				throw new InvalidOperationException("This method cannot be used until Complete() has been called.");
			}
		}

		private void AssertNotComplete()
		{
			if (_complete)
			{
				throw new InvalidOperationException("Once Complete() has been called, items cannot be added");
			}
		}
	}
}
