using Mono.Cecil;
using Unity.IL2CPP.Contexts.Services;

namespace Unity.IL2CPP.Tiny
{
	public static class TinyNamingExtensions
	{
		public static string TinyTypeOffsetNameFor(this INamingService naming, TypeReference type)
		{
			return "TINY_TYPE_OFFSET_" + naming.ForRuntimeUniqueTypeNameOnly(type);
		}

		public static string TinyStringOffsetNameFor(this INamingService naming, string literal)
		{
			return "TINY_STRING_OFFSET_" + naming.ForStringLiteralIdentifier(literal);
		}
	}
}
