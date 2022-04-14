using Mono.Cecil;
using Unity.IL2CPP.Contexts;

namespace Unity.IL2CPP.GenericsCollection
{
	internal static class ArrayRegistration
	{
		public static bool ShouldForce2DArrayFor(ReadOnlyContext context, TypeDefinition type)
		{
			if (context.Global.Parameters.UsingTinyBackend)
			{
				return false;
			}
			return type.MetadataType == MetadataType.Single;
		}
	}
}
