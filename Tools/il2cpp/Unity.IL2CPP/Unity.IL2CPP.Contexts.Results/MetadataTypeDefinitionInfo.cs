using Unity.IL2CPP.Metadata.RuntimeTypes;

namespace Unity.IL2CPP.Contexts.Results
{
	public class MetadataTypeDefinitionInfo : MetadataIndex
	{
		public readonly IIl2CppRuntimeType RuntimeType;

		public readonly IIl2CppRuntimeType BaseRuntimeType;

		public readonly IIl2CppRuntimeType DeclaringRuntimeType;

		public readonly IIl2CppRuntimeType ElementRuntimeType;

		public MetadataTypeDefinitionInfo(int index, IIl2CppRuntimeType runtimeType, IIl2CppRuntimeType baseRuntimeType, IIl2CppRuntimeType declaringRuntimeType, IIl2CppRuntimeType elementRuntimeType)
			: base(index)
		{
			RuntimeType = runtimeType;
			BaseRuntimeType = baseRuntimeType;
			DeclaringRuntimeType = declaringRuntimeType;
			ElementRuntimeType = elementRuntimeType;
		}
	}
}
