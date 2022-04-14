using Mono.Cecil;
using Unity.Cecil.Awesome;
using Unity.Cecil.Awesome.Comparers;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.Marshaling.BodyWriters.NativeToManaged
{
	internal class ProjectedMethodBodyWriter : ComCallableWrapperMethodBodyWriter
	{
		private readonly MethodReference _actualManagedMethod;

		protected override string ManagedObjectExpression => "__this";

		public ProjectedMethodBodyWriter(MinimalContext context, MethodReference managedMethod, MethodReference nativeInterfaceMethod)
			: base(context, nativeInterfaceMethod, nativeInterfaceMethod, MarshalType.WindowsRuntime)
		{
			_actualManagedMethod = managedMethod;
		}

		protected override void WriteMethodCallStatement(IRuntimeMetadataAccess metadataAccess, string thisArgument, string[] localVariableNames, IGeneratedMethodCodeWriter writer, string returnVariable = null)
		{
			MethodCallType methodCallType = (_actualManagedMethod.DeclaringType.IsInterface() ? MethodCallType.Virtual : MethodCallType.Normal);
			if (_actualManagedMethod.DeclaringType.IsValueType())
			{
				thisArgument = "(" + writer.Context.Global.Services.Naming.ForTypeNameOnly(_actualManagedMethod.DeclaringType) + "*)UnBox(" + thisArgument + ", " + metadataAccess.TypeInfoFor(_actualManagedMethod.DeclaringType) + ")";
			}
			string text = returnVariable;
			TypeReference typeReference = TypeResolver.For(_actualManagedMethod.DeclaringType, _actualManagedMethod).ResolveReturnType(_actualManagedMethod);
			TypeReference b = TypeResolver.For(_managedMethod.DeclaringType, _managedMethod).ResolveReturnType(_managedMethod);
			bool flag = TypeReferenceEqualityComparer.AreEqual(typeReference, b);
			writer.WriteCommentedLine(_actualManagedMethod.FullName);
			if (!flag)
			{
				text = returnVariable + "ExactType";
				writer.WriteStatement(writer.Context.Global.Services.Naming.ForVariable(typeReference) + " " + text);
			}
			WriteMethodCallStatementWithResult(metadataAccess, thisArgument, _actualManagedMethod, methodCallType, writer, text, localVariableNames);
			if (!string.IsNullOrEmpty(returnVariable) && !flag)
			{
				writer.WriteStatement(returnVariable + " = " + text);
			}
		}
	}
}
