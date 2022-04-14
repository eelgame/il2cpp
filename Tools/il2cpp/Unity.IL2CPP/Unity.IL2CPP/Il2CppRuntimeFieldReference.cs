using Mono.Cecil;
using Unity.IL2CPP.Metadata.RuntimeTypes;

namespace Unity.IL2CPP
{
	public class Il2CppRuntimeFieldReference
	{
		public readonly FieldReference Field;

		public readonly IIl2CppRuntimeType DeclaringTypeData;

		public Il2CppRuntimeFieldReference(FieldReference field, IIl2CppRuntimeType declaringTypeData)
		{
			Field = field;
			DeclaringTypeData = declaringTypeData;
		}

		public override bool Equals(object obj)
		{
			return Field.Equals(obj);
		}

		public override int GetHashCode()
		{
			return Field.GetHashCode();
		}

		public override string ToString()
		{
			return Field.ToString();
		}
	}
}
