using Unity.IL2CPP.Metadata.RuntimeTypes;

namespace Unity.IL2CPP.Metadata
{
	public class ParameterDefaultValue
	{
		public readonly int ParameterIndex;

		public readonly IIl2CppRuntimeType DeclaringType;

		public readonly int DataIndex;

		public ParameterDefaultValue(int parameterIndex, IIl2CppRuntimeType declaringType, int dataIndex)
		{
			ParameterIndex = parameterIndex;
			DeclaringType = declaringType;
			DataIndex = dataIndex;
		}

		public override string ToString()
		{
			return $"{{ {ParameterIndex}, {DeclaringType}, {DataIndex} }}";
		}
	}
}
