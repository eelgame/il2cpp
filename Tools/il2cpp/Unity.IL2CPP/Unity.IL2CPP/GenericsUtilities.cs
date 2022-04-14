using Mono.Cecil;
using Unity.IL2CPP.Contexts;

namespace Unity.IL2CPP
{
	public static class GenericsUtilities
	{
		public static bool CheckForMaximumRecursion(ReadOnlyContext context, IGenericInstance genericInstance)
		{
			return RecursiveGenericDepthFor(genericInstance) >= context.Global.InputData.MaximumRecursiveGenericDepth;
		}

		public static int RecursiveGenericDepthFor(IGenericInstance genericInstance)
		{
			if (genericInstance == null)
			{
				return 0;
			}
			return RecursiveGenericDepthFor(genericInstance, genericInstance.HasGenericArguments ? 1 : 0);
		}

		private static int RecursiveGenericDepthFor(IGenericInstance instance, int depth)
		{
			int num = 0;
			foreach (TypeReference genericArgument in instance.GenericArguments)
			{
				num = MaximumDepthFor(depth, genericArgument, num);
			}
			return depth + num;
		}

		private static int RecursiveGenericDepthFor(ArrayType type, int depth)
		{
			return depth + MaximumDepthFor(depth, type.ElementType, 0);
		}

		private static int MaximumDepthFor(int depth, TypeReference genericArgument, int maximumDepth)
		{
			if (genericArgument is GenericInstanceType)
			{
				int num = RecursiveGenericDepthFor(genericArgument as GenericInstanceType, depth);
				if (num > maximumDepth)
				{
					maximumDepth = num;
				}
			}
			else if (genericArgument is ArrayType)
			{
				int num2 = RecursiveGenericDepthFor(genericArgument as ArrayType, depth);
				if (num2 > maximumDepth)
				{
					maximumDepth = num2;
				}
			}
			return maximumDepth;
		}

		public static bool IsGenericInstanceOfCompareExchange(MethodReference methodReference)
		{
			if (methodReference.DeclaringType.Name == "Interlocked" && methodReference.Name == "CompareExchange")
			{
				return methodReference.IsGenericInstance;
			}
			return false;
		}

		public static bool IsGenericInstanceOfExchange(MethodReference methodReference)
		{
			if (methodReference.DeclaringType.Name == "Interlocked" && methodReference.Name == "Exchange")
			{
				return methodReference.IsGenericInstance;
			}
			return false;
		}
	}
}
