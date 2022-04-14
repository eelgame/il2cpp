using Unity.IL2CPP.Metadata.RuntimeTypes;

namespace Unity.IL2CPP.Contexts.Results
{
	public class MetadataFieldInfo : MetadataIndex
	{
		public readonly IIl2CppRuntimeType FieldType;

		public MetadataFieldInfo(int index, IIl2CppRuntimeType fieldType)
			: base(index)
		{
			FieldType = fieldType;
		}
	}
}
