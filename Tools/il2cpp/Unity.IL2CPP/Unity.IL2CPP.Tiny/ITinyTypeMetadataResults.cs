using System.Collections.ObjectModel;
using Mono.Cecil;

namespace Unity.IL2CPP.Tiny
{
	public interface ITinyTypeMetadataResults
	{
		ReadOnlyCollection<TinyTypeEntry> GetAllEntries();

		TinyTypeEntry GetTypeEntry(TypeReference type);
	}
}
