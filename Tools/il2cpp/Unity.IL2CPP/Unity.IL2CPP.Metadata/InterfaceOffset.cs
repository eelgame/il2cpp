using Unity.IL2CPP.Metadata.RuntimeTypes;

namespace Unity.IL2CPP.Metadata
{
	public struct InterfaceOffset
	{
		public readonly IIl2CppRuntimeType RuntimeType;

		public readonly int Offset;

		public InterfaceOffset(IIl2CppRuntimeType runtimeType, int offset)
		{
			RuntimeType = runtimeType;
			Offset = offset;
		}
	}
}
