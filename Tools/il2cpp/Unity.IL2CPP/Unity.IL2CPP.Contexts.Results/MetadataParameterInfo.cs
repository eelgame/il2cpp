using Unity.IL2CPP.Metadata.RuntimeTypes;

namespace Unity.IL2CPP.Contexts.Results
{
	public class MetadataParameterInfo : MetadataIndex
	{
		public readonly IIl2CppRuntimeType ParameterType;

		public MetadataParameterInfo(int index, IIl2CppRuntimeType parameterType)
			: base(index)
		{
			ParameterType = parameterType;
		}
	}
}
