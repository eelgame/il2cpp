namespace Unity.IL2CPP.GenericsCollection.CodeFlow
{
	internal struct Node<T>
	{
		public readonly T Item;

		public readonly int MethodDependenciesStartIndex;

		public readonly int MethodDependenciesEndIndex;

		public readonly int TypeDependenciesStartIndex;

		public readonly int TypeDependenciesEndIndex;

		public Node(T item, int methodDependenciesStartIndex, int methodDependenciesEndIndex, int typeDependenciesStartIndex, int typeDependenciesEndIndex)
		{
			Item = item;
			MethodDependenciesStartIndex = methodDependenciesStartIndex;
			MethodDependenciesEndIndex = methodDependenciesEndIndex;
			TypeDependenciesStartIndex = typeDependenciesStartIndex;
			TypeDependenciesEndIndex = typeDependenciesEndIndex;
		}
	}
}
