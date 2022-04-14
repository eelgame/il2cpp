using Mono.Cecil;

namespace Unity.IL2CPP.GenericsCollection.CodeFlow
{
	internal interface IHasDefinition
	{
		IMemberDefinition GetDefinition();
	}
}
