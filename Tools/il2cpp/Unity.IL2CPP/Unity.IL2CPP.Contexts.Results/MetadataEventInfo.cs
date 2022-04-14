using Unity.IL2CPP.Metadata.RuntimeTypes;

namespace Unity.IL2CPP.Contexts.Results
{
	public class MetadataEventInfo : MetadataIndex
	{
		public readonly IIl2CppRuntimeType EventType;

		public MetadataEventInfo(int index, IIl2CppRuntimeType eventType)
			: base(index)
		{
			EventType = eventType;
		}
	}
}
