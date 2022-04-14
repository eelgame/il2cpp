using Mono.Cecil;

namespace Unity.IL2CPP.Metadata.RuntimeTypes
{
	public interface IIl2CppRuntimeType
	{
		TypeReference Type { get; }

		int Attrs { get; }
	}
}
