using Mono.Cecil;
using Unity.Cecil.Awesome;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.Marshaling.BodyWriters.NativeToManaged
{
	public abstract class NativeToManagedInteropMethodBodyWriter : InteropMethodBodyWriter
	{
		protected readonly MethodReference _managedMethod;

		public NativeToManagedInteropMethodBodyWriter(MinimalContext context, MethodReference managedMethod, MethodReference interopMethod, MarshalType marshalType, bool useUnicodeCharset)
			: base(context, interopMethod, managedMethod, new NativeToManagedMarshaler(context, TypeResolver.For(interopMethod.DeclaringType, interopMethod), marshalType, useUnicodeCharset))
		{
			_managedMethod = managedMethod;
		}

		protected override void WriteMethodPrologue(IGeneratedMethodCodeWriter writer, IRuntimeMetadataAccess metadataAccess)
		{
			if (_context.Global.Parameters.UsingTinyBackend)
			{
				writer.WriteLine("tiny::vm::ScopedThreadAttacher _vmThreadHelper;");
			}
			else
			{
				writer.WriteLine("il2cpp::vm::ScopedThreadAttacher _vmThreadHelper;");
			}
			writer.WriteLine();
		}

		protected virtual void WriteMethodCallStatement(IRuntimeMetadataAccess metadataAccess, string thisArgument, string[] localVariableNames, IGeneratedMethodCodeWriter writer, string returnVariable = null)
		{
			MethodCallType methodCallType = (_managedMethod.DeclaringType.IsInterface() ? MethodCallType.Virtual : MethodCallType.Normal);
			WriteMethodCallStatementWithResult(metadataAccess, thisArgument, _managedMethod, methodCallType, writer, returnVariable, localVariableNames);
		}

		protected void WriteMethodCallStatement(IRuntimeMetadataAccess metadataAccess, string thisVariableName, MethodReference method, MethodCallType methodCallType, IGeneratedMethodCodeWriter writer, params string[] args)
		{
			writer.WriteMethodCallStatement(metadataAccess, thisVariableName, _managedMethod, method, methodCallType, args);
		}

		protected void WriteMethodCallStatementWithResult(IRuntimeMetadataAccess metadataAccess, string thisVariableName, MethodReference method, MethodCallType methodCallType, IGeneratedMethodCodeWriter writer, string returnVariable, params string[] args)
		{
			if (returnVariable != null)
			{
				writer.WriteMethodCallWithResultStatement(metadataAccess, thisVariableName, _managedMethod, method, methodCallType, returnVariable, args);
			}
			else
			{
				writer.WriteMethodCallStatement(metadataAccess, thisVariableName, _managedMethod, method, methodCallType, args);
			}
		}

		protected sealed override void WriteReturnStatement(IGeneratedMethodCodeWriter writer, string unmarshaledReturnValueVariableName, IRuntimeMetadataAccess metadataAccess)
		{
			MarshaledType[] marshaledTypes = MarshalInfoWriterFor(GetMethodReturnType()).MarshaledTypes;
			for (int i = 0; i < marshaledTypes.Length - 1; i++)
			{
				writer.WriteLine("*{0}{2} = {1}{2};", _context.Global.Services.Naming.ForComInterfaceReturnParameterName(), unmarshaledReturnValueVariableName, marshaledTypes[i].VariableName);
			}
			WriteReturnStatementEpilogue(writer, unmarshaledReturnValueVariableName);
		}

		protected abstract void WriteReturnStatementEpilogue(IGeneratedMethodCodeWriter writer, string unmarshaleedReturnValueVariableName);
	}
}
