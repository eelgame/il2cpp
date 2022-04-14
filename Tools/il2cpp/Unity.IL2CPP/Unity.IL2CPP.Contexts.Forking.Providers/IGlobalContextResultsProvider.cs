using Unity.IL2CPP.Contexts.Results;

namespace Unity.IL2CPP.Contexts.Forking.Providers
{
	public interface IGlobalContextResultsProvider
	{
		ICppDeclarationsCache CppDeclarationsCache { get; }
	}
}
