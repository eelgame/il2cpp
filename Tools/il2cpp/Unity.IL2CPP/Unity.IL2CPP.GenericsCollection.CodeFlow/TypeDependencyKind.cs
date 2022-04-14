using System;

namespace Unity.IL2CPP.GenericsCollection.CodeFlow
{
	[Flags]
	internal enum TypeDependencyKind
	{
		None = 0,
		InstantiatedGenericInstance = 1,
		InstantiatedArray = 2,
		MethodParameterOrReturnType = 4,
		BaseTypeOrInterface = 8,
		ImplicitDependency = 0x10,
		IsOfInterest = 0x20
	}
}
