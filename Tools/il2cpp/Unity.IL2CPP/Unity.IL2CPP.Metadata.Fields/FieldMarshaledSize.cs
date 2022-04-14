using Unity.IL2CPP.Metadata.RuntimeTypes;

namespace Unity.IL2CPP.Metadata.Fields
{
	public class FieldMarshaledSize
	{
		public readonly int FieldIndex;

		public readonly IIl2CppRuntimeType RuntimeType;

		public readonly int Size;

		public FieldMarshaledSize(int fieldIndex, IIl2CppRuntimeType runtimeType, int size)
		{
			FieldIndex = fieldIndex;
			RuntimeType = runtimeType;
			Size = size;
		}

		public override string ToString()
		{
			return $"{{ {FieldIndex}, {RuntimeType}, {Size} }}";
		}
	}
}
