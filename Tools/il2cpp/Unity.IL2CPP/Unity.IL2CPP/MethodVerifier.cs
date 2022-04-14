using Mono.Cecil;

namespace Unity.IL2CPP
{
	internal static class MethodVerifier
	{
		public static bool IsNonGenericMethodThatDoesntExist(MethodReference method)
		{
			if (!method.IsGenericInstance && !method.DeclaringType.IsGenericInstance)
			{
				TypeDefinition typeDefinition = method.DeclaringType.Resolve();
				MethodDefinition methodDefinition = method.Resolve();
				return typeDefinition != methodDefinition.DeclaringType;
			}
			return false;
		}
	}
}
