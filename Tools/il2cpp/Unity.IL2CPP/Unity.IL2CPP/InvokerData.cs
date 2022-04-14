using System;

namespace Unity.IL2CPP
{
	public struct InvokerData : IEquatable<InvokerData>
	{
		public readonly bool VoidReturn;

		public readonly int ParameterCount;

		public InvokerData(bool voidReturn, int parameterCount)
		{
			VoidReturn = voidReturn;
			ParameterCount = parameterCount;
		}

		public override int GetHashCode()
		{
			bool voidReturn = VoidReturn;
			int hashCode = voidReturn.GetHashCode();
			int parameterCount = ParameterCount;
			return hashCode ^ parameterCount.GetHashCode();
		}

		public bool Equals(InvokerData other)
		{
			if (VoidReturn == other.VoidReturn)
			{
				return ParameterCount == other.ParameterCount;
			}
			return false;
		}
	}
}
