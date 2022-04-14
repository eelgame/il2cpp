namespace Unity.IL2CPP.GenericsCollection.CodeFlow
{
	internal struct StagingNode<T>
	{
		public readonly T Item;

		public readonly int MethodDependenciesStartIndex;

		public readonly int MethodDependenciesEndIndex;

		public readonly int TypeDependenciesStartIndex;

		public readonly int TypeDependenciesEndIndex;

		public bool IsNeeded;

		public StagingNode(T item, int methodDependenciesStartIndex, int methodDependenciesEndIndex, int typeDependenciesStartIndex, int typeDependenciesEndIndex, bool isNeeded = false)
		{
			Item = item;
			MethodDependenciesStartIndex = methodDependenciesStartIndex;
			MethodDependenciesEndIndex = methodDependenciesEndIndex;
			TypeDependenciesStartIndex = typeDependenciesStartIndex;
			TypeDependenciesEndIndex = typeDependenciesEndIndex;
			IsNeeded = isNeeded;
		}
	}
}
