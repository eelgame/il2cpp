using System.Collections.Generic;
using Mono.Cecil;
using Unity.Cecil.Awesome.Comparers;

namespace Unity.IL2CPP.GenericsCollection
{
	public class InflatedCollectionCollector
	{
		public readonly InflatedCollection<GenericInstanceType> Types = new InflatedCollection<GenericInstanceType>(new TypeReferenceEqualityComparer());

		public readonly InflatedCollection<GenericInstanceType> TypeDeclarations = new InflatedCollection<GenericInstanceType>(new TypeReferenceEqualityComparer());

		public readonly InflatedCollection<TypeReference> InstantiatedGenericsAndArrays = new InflatedCollection<TypeReference>(new TypeReferenceEqualityComparer());

		public readonly InflatedCollection<GenericInstanceMethod> Methods = new InflatedCollection<GenericInstanceMethod>(new MethodReferenceComparer());

		public readonly HashSet<ArrayType> VisitedArrays = new HashSet<ArrayType>(new TypeReferenceEqualityComparer());

		public ReadOnlyInflatedCollectionCollector AsReadOnly()
		{
			return new ReadOnlyInflatedCollectionCollector(this);
		}
	}
}
