using Mono.Cecil;

namespace Unity.IL2CPP.Contexts.Results
{
	public interface ICppDeclarationsCache
	{
		ICppDeclarations GetDeclarations(TypeReference type);

		string GetSource(TypeReference type);

		bool TryGetValue(TypeReference type, out CppDeclarationsCache.CacheData data);
	}
}
