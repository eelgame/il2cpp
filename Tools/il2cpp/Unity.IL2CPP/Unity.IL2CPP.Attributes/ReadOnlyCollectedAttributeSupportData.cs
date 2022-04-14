using System.Collections.ObjectModel;

namespace Unity.IL2CPP.Attributes
{
	public class ReadOnlyCollectedAttributeSupportData
	{
		internal readonly ReadOnlyCollection<AttributeData> AttributeData;

		internal readonly AttributeCollection AttributeCollection;

		public ReadOnlyCollectedAttributeSupportData(ReadOnlyCollection<AttributeData> attributeData, AttributeCollection attributeCollection)
		{
			AttributeData = attributeData;
			AttributeCollection = attributeCollection;
		}
	}
}
