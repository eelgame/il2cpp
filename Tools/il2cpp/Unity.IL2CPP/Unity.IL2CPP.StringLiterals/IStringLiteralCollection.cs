using System.Collections.ObjectModel;

namespace Unity.IL2CPP.StringLiterals
{
	public interface IStringLiteralCollection
	{
		ReadOnlyCollection<string> GetStringLiterals();

		ReadOnlyCollection<StringMetadataToken> GetStringMetadataTokens();

		int GetIndex(StringMetadataToken literal);
	}
}
