using Mono.Cecil;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Marshaling.BodyWriters.NativeToManaged;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.WindowsRuntime
{
	internal class AddOrRemoveCanExecuteChangedMethodBodyWriter : ProjectedMethodBodyWriter
	{
		private readonly MethodDefinition _helperMethod;

		public AddOrRemoveCanExecuteChangedMethodBodyWriter(MinimalContext context, MethodReference method, MethodDefinition helperMethod)
			: base(context, method, method)
		{
			_helperMethod = helperMethod;
		}

		protected override void WriteInteropCallStatementWithinTryBlock(IGeneratedMethodCodeWriter writer, string[] localVariableNames, IRuntimeMetadataAccess metadataAccess)
		{
			if (InteropMethod.ReturnType.MetadataType != MetadataType.Void)
			{
				WriteMethodCallStatementWithResult(metadataAccess, "NULL", _helperMethod, MethodCallType.Normal, writer, _context.Global.Services.Naming.ForInteropReturnValue(), ManagedObjectExpression, localVariableNames[0]);
			}
			else
			{
				WriteMethodCallStatement(metadataAccess, "NULL", _helperMethod, MethodCallType.Normal, writer, ManagedObjectExpression, localVariableNames[0]);
			}
		}
	}
}
