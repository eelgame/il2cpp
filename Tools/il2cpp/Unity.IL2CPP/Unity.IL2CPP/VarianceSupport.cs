using Mono.Cecil;
using Unity.Cecil.Awesome;
using Unity.Cecil.Awesome.Comparers;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP
{
	public sealed class VarianceSupport
	{
		public static bool IsNeededForConversion(TypeReference leftType, TypeReference rightType)
		{
			leftType = leftType.WithoutModifiers();
			rightType = rightType.WithoutModifiers();
			if (leftType.IsFunctionPointer || rightType.IsFunctionPointer)
			{
				return false;
			}
			if (leftType.IsByReference && rightType.IsPointer && !TypeReferenceEqualityComparer.AreEqual(leftType, rightType))
			{
				return true;
			}
			if (leftType.IsByReference || rightType.IsByReference)
			{
				return false;
			}
			if (TypeReferenceEqualityComparer.AreEqual(leftType, rightType))
			{
				return false;
			}
			if (leftType.IsDelegate() && rightType.IsDelegate())
			{
				return true;
			}
			if (!leftType.IsArray)
			{
				return rightType.IsArray;
			}
			return true;
		}

		public static string Apply(ReadOnlyContext context, TypeReference leftType, TypeReference rightType)
		{
			if (TypeReferenceEqualityComparer.AreEqual(leftType, rightType))
			{
				return string.Empty;
			}
			return "(" + context.Global.Services.Naming.ForVariable(leftType) + ")";
		}
	}
}
