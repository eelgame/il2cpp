using Mono.Cecil;

namespace Unity.IL2CPP.Attributes
{
	public struct AttributeData
	{
		public readonly uint MetadataToken;

		public readonly string FunctionName;

		public readonly CustomAttribute[] AttributeTypes;

		public AttributeData(uint metadataToken, string functionName, CustomAttribute[] attributeTypes)
		{
			MetadataToken = metadataToken;
			FunctionName = functionName;
			AttributeTypes = attributeTypes;
		}
	}
}
