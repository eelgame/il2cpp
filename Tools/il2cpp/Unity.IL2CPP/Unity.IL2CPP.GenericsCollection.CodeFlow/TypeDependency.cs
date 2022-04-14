using Mono.Cecil;

namespace Unity.IL2CPP.GenericsCollection.CodeFlow
{
	internal struct TypeDependency
	{
		public readonly TypeReference Type;

		public readonly int DefinitionIndex;

		public readonly TypeDependencyKind Kind;

		public TypeDependency(TypeReference type, int definitionIndex, TypeDependencyKind kind)
		{
			Type = type;
			DefinitionIndex = definitionIndex;
			Kind = kind;
		}
	}
}
