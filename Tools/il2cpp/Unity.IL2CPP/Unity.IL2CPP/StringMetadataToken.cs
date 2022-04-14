using Mono.Cecil;

namespace Unity.IL2CPP
{
	public class StringMetadataToken
	{
		public string Literal { get; private set; }

		public AssemblyDefinition Assembly { get; private set; }

		public MetadataToken Token { get; private set; }

		public StringMetadataToken(string literal, AssemblyDefinition assembly, MetadataToken token)
		{
			Literal = literal;
			Assembly = assembly;
			Token = token;
		}
	}
}
