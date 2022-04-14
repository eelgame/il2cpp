using System.Collections.ObjectModel;

namespace Unity.IL2CPP
{
	public interface IFieldReferenceCollection
	{
		ReadOnlyDictionary<Il2CppRuntimeFieldReference, uint> Fields { get; }
	}
}
