using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Unity.IL2CPP.Tiny
{
	public interface ITinyStringMetadataResults
	{
		int GetStringLiteralCount();

		ReadOnlyCollection<StringLiteralEntry> GetEntries();

		IEnumerable<string> GetStringLines32();

		IEnumerable<string> GetStringLines64();
	}
}
