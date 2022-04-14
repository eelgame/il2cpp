using Unity.IL2CPP.Common;

namespace Unity.IL2CPP
{
	public interface ICppDeclarationsBasic
	{
		ReadOnlyHashSet<string> Includes { get; }

		ReadOnlyHashSet<string> RawTypeForwardDeclarations { get; }

		ReadOnlyHashSet<string> RawMethodForwardDeclarations { get; }
	}
}
