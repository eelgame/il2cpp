using Unity.IL2CPP.Metadata.RuntimeTypes;

namespace Unity.IL2CPP.Metadata.Fields
{
	public class FieldDefaultValue
	{
		public readonly int FieldIndex;

		public readonly IIl2CppRuntimeType RuntimeType;

		public readonly int DataIndex;

		public FieldDefaultValue(int fieldIndex, IIl2CppRuntimeType runtimeType, int dataIndex)
		{
			FieldIndex = fieldIndex;
			RuntimeType = runtimeType;
			DataIndex = dataIndex;
		}

		public override string ToString()
		{
			return $"{{ {FieldIndex}, {RuntimeType}, {DataIndex} }}";
		}
	}
}
