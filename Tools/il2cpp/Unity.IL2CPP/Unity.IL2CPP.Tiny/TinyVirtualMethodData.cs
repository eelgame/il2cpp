using Mono.Cecil;

namespace Unity.IL2CPP.Tiny
{
	internal class TinyVirtualMethodData
	{
		public MethodReference VirtualMethod;

		public TypeReference DerivedDeclaringType;

		public override bool Equals(object obj)
		{
			if (!(obj is TinyVirtualMethodData tinyVirtualMethodData))
			{
				return base.Equals(obj);
			}
			if (VirtualMethod.Equals(tinyVirtualMethodData.VirtualMethod))
			{
				return DerivedDeclaringType.Equals(tinyVirtualMethodData.DerivedDeclaringType);
			}
			return false;
		}

		public override int GetHashCode()
		{
			int num = 0;
			if (VirtualMethod != null)
			{
				num = VirtualMethod.GetHashCode();
			}
			if (DerivedDeclaringType != null)
			{
				num += DerivedDeclaringType.GetHashCode();
			}
			return num;
		}
	}
}
