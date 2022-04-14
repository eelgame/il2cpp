using Mono.Cecil;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP
{
	public struct StackInfo
	{
		public readonly string Expression;

		public readonly TypeReference Type;

		public readonly TypeReference BoxedType;

		public readonly MethodReference MethodExpressionIsPointingTo;

		public StackInfo(string expression, TypeReference type, TypeReference boxedType = null, MethodReference methodExpressionIsPointingTo = null)
		{
			Expression = expression;
			Type = type;
			BoxedType = boxedType;
			MethodExpressionIsPointingTo = methodExpressionIsPointingTo;
		}

		public StackInfo(StackInfo local)
		{
			Expression = local.Expression;
			Type = local.Type;
			BoxedType = local.BoxedType;
			MethodExpressionIsPointingTo = local.MethodExpressionIsPointingTo;
		}

		public override string ToString()
		{
			return Expression;
		}

		public string GetIdentifierExpression(ReadOnlyContext context)
		{
			return context.Global.Services.Naming.ForVariable(Type) + " " + Expression;
		}
	}
}
