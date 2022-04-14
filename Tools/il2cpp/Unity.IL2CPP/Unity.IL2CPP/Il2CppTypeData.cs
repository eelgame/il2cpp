using Mono.Cecil;

namespace Unity.IL2CPP
{
	public struct Il2CppTypeData
	{
		public readonly int Attrs;

		public readonly TypeReference Type;

		public Il2CppTypeData(TypeReference type, int attrs)
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
