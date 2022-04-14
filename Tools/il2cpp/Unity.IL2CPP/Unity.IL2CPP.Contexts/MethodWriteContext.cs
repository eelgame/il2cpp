using Mono.Cecil;
using Unity.Cecil.Awesome;

namespace Unity.IL2CPP.Contexts
{
	public class MethodWriteContext
	{
		public readonly GlobalWriteContext Global;

		public readonly AssemblyWriteContext Assembly;

		public readonly MethodReference MethodReference;

		public readonly MethodDefinition MethodDefinition;

		public readonly TypeResolver TypeResolver;

		public readonly TypeReference ResolvedReturnType;

		public SourceWritingContext SourceWritingContext => Assembly.SourceWritingContext;

		public MethodWriteContext(AssemblyWriteContext assembly, MethodReference method)
		{
			Global = assembly.Global;
			Assembly = assembly;
			MethodReference = method;
			MethodDefinition = method.Resolve();
			TypeResolver = new TypeResolver(method.DeclaringType as GenericInstanceType, method as GenericInstanceMethod);
			ResolvedReturnType = TypeResolver.Resolve(GenericParameterResolver.ResolveReturnTypeIfNeeded(MethodReference));
		}

		public MinimalContext AsMinimal()
		{
			return Global.CreateMinimalContext();
		}

		public ReadOnlyContext AsReadonly()
		{
			return Global.GetReadOnlyContext();
		}

		public static implicit operator ReadOnlyContext(MethodWriteContext c)
		{
			return c.AsReadonly();
		}

		public static implicit operator MinimalContext(MethodWriteContext c)
		{
			return c.AsMinimal();
		}
	}
}
