using System.Collections.Generic;
using Unity.Cecil.Awesome.Comparers;

namespace Unity.IL2CPP
{
	public class VirtualMethodDeclarationDataComparer : IEqualityComparer<VirtualMethodDeclarationData>
	{
		public bool Equals(VirtualMethodDeclarationData left, VirtualMethodDeclarationData right)
		{
			if (MethodReferenceComparer.AreEqual(left.Method, right.Method) && ((left.DeclaringTypeIsInterface == right.DeclaringTypeIsInterface) & (left.HasGenericParameters == right.HasGenericParameters)) && left.ReturnsVoid == right.ReturnsVoid)
			{
				return left.NumberOfParameters == right.NumberOfParameters;
			}
			return false;
		}

		public int GetHashCode(VirtualMethodDeclarationData data)
		{
			return data.GetHashCode();
		}
	}
}
