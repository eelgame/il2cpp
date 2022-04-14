using Unity.IL2CPP.Metadata.RuntimeTypes;

namespace Unity.IL2CPP.Debugger
{
	public struct VariableData
	{
		public readonly IIl2CppRuntimeType type;

		public readonly int NameIndex;

		public readonly int ScopeIndex;

		public VariableData(IIl2CppRuntimeType type, int nameIndex, int scopeIndex)
		{
			this.type = type;
			NameIndex = nameIndex;
			ScopeIndex = scopeIndex;
		}
	}
}
