using System;
using Mono.Cecil;
using Unity.Cecil.Awesome.Comparers;

namespace Unity.IL2CPP.GenericsCollection.CodeFlow
{
	internal struct TypeReferenceDefinitionPair : IEquatable<TypeReferenceDefinitionPair>, IHasDefinition
	{
		public readonly TypeDefinition Definition;

		public readonly TypeReference Reference;

		public readonly TypeDependencyKind Kind;

		public TypeReferenceDefinitionPair(TypeDefinition definition, TypeReference reference, TypeDependencyKind kind)
		{
			Definition = definition;
			Reference = reference;
			Kind = kind;
		}

		public bool Equals(TypeReferenceDefinitionPair other)
		{
			if (Definition != other.Definition)
			{
				return false;
			}
			if (Kind != other.Kind)
			{
				return false;
			}
			return TypeReferenceEqualityComparer.AreEqual(Reference, other.Reference);
		}

		public IMemberDefinition GetDefinition()
		{
			return Definition;
		}

		public override int GetHashCode()
		{
			return TypeReferenceEqualityComparer.GetHashCodeFor(Reference);
		}
	}
}
