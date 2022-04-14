using Mono.Cecil;

namespace Unity.IL2CPP.Metadata.RuntimeTypes
{
	public class Il2CppRuntimeTypeBase<T> : IIl2CppRuntimeType where T : TypeReference
	{
		public readonly T Type;

		public int Attrs { get; }

		TypeReference IIl2CppRuntimeType.Type => Type;

		public Il2CppRuntimeTypeBase(T type, int attrs)
		{
			Type = type;
			Attrs = attrs;
		}

		public override string ToString()
		{
			return $"{Type.FullName} [{Attrs}]";
		}
	}
}
