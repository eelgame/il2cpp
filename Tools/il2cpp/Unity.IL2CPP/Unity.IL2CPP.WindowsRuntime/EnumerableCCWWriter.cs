using System.Linq;
using Mono.Cecil;
using Unity.Cecil.Awesome;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Marshaling.BodyWriters.NativeToManaged;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.WindowsRuntime
{
	internal sealed class EnumerableCCWWriter : IProjectedComCallableWrapperMethodWriter
	{
		private sealed class FirstMethodBodyWriter : ProjectedMethodBodyWriter
		{
			private readonly string _adapterTypeName;

			protected override bool IsReturnValueMarshaled => false;

			public FirstMethodBodyWriter(MinimalContext context, MethodReference getEnumeratorMethod, MethodReference firstMethod, TypeReference iteratorType)
				: base(context, getEnumeratorMethod, firstMethod)
			{
				_adapterTypeName = context.Global.Services.Naming.ForWindowsRuntimeAdapterClass(iteratorType);
			}

			protected override void WriteInteropCallStatementWithinTryBlock(IGeneratedMethodCodeWriter writer, string[] localVariableNames, IRuntimeMetadataAccess metadataAccess)
			{
				writer.WriteLine(_context.Global.Services.Naming.ForVariable(_typeResolver.Resolve(_managedMethod.ReturnType)) + " " + _context.Global.Services.Naming.ForInteropReturnValue() + ";");
				base.WriteInteropCallStatementWithinTryBlock(writer, localVariableNames, metadataAccess);
				string text = Emit.Call(_adapterTypeName + "::__CreateInstance", _context.Global.Services.Naming.ForInteropReturnValue());
				writer.WriteStatement(Emit.Assign("*" + _context.Global.Services.Naming.ForComInterfaceReturnParameterName(), "(" + _context.Global.Services.Naming.ForInteropReturnValue() + " != NULL) ? " + text + " : NULL"));
			}
		}

		public void WriteDependenciesFor(SourceWritingContext context, IGeneratedMethodCodeWriter writer, TypeReference interfaceType)
		{
			if (interfaceType.Resolve().HasGenericParameters)
			{
				GenericEnumeratorToIteratorAdapterWriter.WriteDefinitions(context, writer, (GenericInstanceType)interfaceType);
			}
			else
			{
				EnumeratorToBindableIteratorAdapterWriter.WriteDefinitions(context, writer);
			}
		}

		public ComCallableWrapperMethodBodyWriter GetBodyWriter(SourceWritingContext context, MethodReference method)
		{
			TypeReference declaringType = method.DeclaringType;
			TypeResolver typeResolver = TypeResolver.For(declaringType);
			TypeReference typeReference = context.Global.Services.WindowsRuntime.ProjectToCLR(declaringType);
			TypeResolver typeResolver2 = TypeResolver.For(typeReference);
			MethodDefinition method2 = typeReference.Resolve().Methods.First((MethodDefinition m) => m.Name == "GetEnumerator");
			MethodReference getEnumeratorMethod = typeResolver2.Resolve(method2);
			TypeReference iteratorType = typeResolver.Resolve(method.ReturnType);
			return new FirstMethodBodyWriter(context, getEnumeratorMethod, method, iteratorType);
		}
	}
}
