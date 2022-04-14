using Mono.Cecil;

namespace Unity.IL2CPP
{
	public struct VirtualMethodDeclarationData
	{
		public MethodReference Method;

		public bool DeclaringTypeIsInterface;

		public bool HasGenericParameters;

		public bool ReturnsVoid;

		public int NumberOfParameters;

		public VirtualMethodDeclarationData(MethodReference method, int numberOfParameters, bool returnsVoid, bool declaringTypeIsInterface, bool methodDefinitionHasGenericParameters)
		{
			Method = method;
			DeclaringTypeIsInterface = declaringTypeIsInterface;
			HasGenericParameters = methodDefinitionHasGenericParameters;
			ReturnsVoid = returnsVoid;
			NumberOfParameters = numberOfParameters;
		}
	}
}
