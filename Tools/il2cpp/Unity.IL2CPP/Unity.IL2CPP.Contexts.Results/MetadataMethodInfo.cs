using Unity.IL2CPP.Metadata.RuntimeTypes;

namespace Unity.IL2CPP.Contexts.Results
{
	public class MetadataMethodInfo : MetadataIndex
	{
		public readonly IIl2CppRuntimeType ReturnType;

		public MetadataMethodInfo(int index, IIl2CppRuntimeType returnType)
			: base(index)
		{
			ReturnType = returnType;
		}
	}
}
