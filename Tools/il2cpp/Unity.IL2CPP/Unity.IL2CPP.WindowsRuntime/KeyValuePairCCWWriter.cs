using System.Linq;
using Mono.Cecil;
using Unity.Cecil.Awesome;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Marshaling.BodyWriters.NativeToManaged;

namespace Unity.IL2CPP.WindowsRuntime
{
	internal class KeyValuePairCCWWriter : IProjectedComCallableWrapperMethodWriter
	{
		private readonly TypeDefinition _keyValuePairTypeDef;

		public KeyValuePairCCWWriter(TypeDefinition keyValuePairTypeDef)
		{
			_keyValuePairTypeDef = keyValuePairTypeDef;
		}

		public void WriteDependenciesFor(SourceWritingContext context, IGeneratedMethodCodeWriter writer, TypeReference interfaceType)
		{
		}

		public ComCallableWrapperMethodBodyWriter GetBodyWriter(SourceWritingContext context, MethodReference interfaceMethod)
		{
			GenericInstanceType genericInstanceType = (GenericInstanceType)interfaceMethod.DeclaringType;
			GenericInstanceType typeReference = new GenericInstanceType(_keyValuePairTypeDef)
			{
				GenericArguments = 
				{
					genericInstanceType.GenericArguments[0],
					genericInstanceType.GenericArguments[1]
				}
			};
			MethodDefinition method = _keyValuePairTypeDef.Methods.Single((MethodDefinition m) => m.Name == interfaceMethod.Name);
			MethodReference managedMethod = TypeResolver.For(typeReference).Resolve(method);
			return new ProjectedMethodBodyWriter(context, managedMethod, interfaceMethod);
		}
	}
}
