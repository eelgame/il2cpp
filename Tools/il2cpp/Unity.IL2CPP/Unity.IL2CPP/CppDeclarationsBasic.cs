using System.Collections.Generic;
using Unity.IL2CPP.Common;

namespace Unity.IL2CPP
{
	public class CppDeclarationsBasic : ICppDeclarationsBasic
	{
		public readonly HashSet<string> _includes = new HashSet<string>();

		public readonly HashSet<string> _rawTypeForwardDeclarations = new HashSet<string>();

		public readonly HashSet<string> _rawMethodForwardDeclarations = new HashSet<string>();

		public ReadOnlyHashSet<string> Includes => _includes.AsReadOnly();

		public ReadOnlyHashSet<string> RawTypeForwardDeclarations => _rawTypeForwardDeclarations.AsReadOnly();

		public ReadOnlyHashSet<string> RawMethodForwardDeclarations => _rawMethodForwardDeclarations.AsReadOnly();
	}
}
