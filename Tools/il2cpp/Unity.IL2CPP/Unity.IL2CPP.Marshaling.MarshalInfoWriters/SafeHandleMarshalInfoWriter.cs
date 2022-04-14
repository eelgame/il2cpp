using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.Marshaling.MarshalInfoWriters
{
	public class SafeHandleMarshalInfoWriter : MarshalableMarshalInfoWriter
	{
		private const string SafeHandleReferenceIncrementedLocalBoolNamePrefix = "___safeHandle_reference_incremented_for";

		private readonly TypeDefinition _safeHandleTypeDefinition;

		private readonly MethodDefinition _addRefMethod;

		private readonly MethodDefinition _releaseMethod;

		private readonly MethodDefinition _defaultConstructor;

		private readonly MarshaledType[] _marshaledTypes;

		public override MarshaledType[] MarshaledTypes => _marshaledTypes;

		public SafeHandleMarshalInfoWriter(ReadOnlyContext context, TypeReference type, TypeDefinition safeHandleTypeTypeDefinition)
			: base(context, type)
		{
			_safeHandleTypeDefinition = safeHandleTypeTypeDefinition;
			_addRefMethod = _safeHandleTypeDefinition.Methods.Single((MethodDefinition method) => method.Name == "DangerousAddRef");
			_releaseMethod = _safeHandleTypeDefinition.Methods.Single((MethodDefinition method) => method.Name == "DangerousRelease");
			_defaultConstructor = _typeRef.Resolve().Methods.SingleOrDefault((MethodDefinition ctor) => ctor.Name == ".ctor" && ctor.Parameters.Count == 0);
			_marshaledTypes = new MarshaledType[1]
			{
				new MarshaledType("void*", "void*")
			};
		}

		public override void WriteMarshalVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, string destinationVariable, string managedVariableName, IRuntimeMetadataAccess metadataAccess)
		{
			writer.WriteLine("if ({0} == {1}) {2};", sourceVariable.Load(), "NULL", Emit.RaiseManagedException($"il2cpp_codegen_get_argument_null_exception(\"{(string.IsNullOrEmpty(managedVariableName) ? sourceVariable.GetNiceName() : managedVariableName)}\")"));
			EmitCallToDangerousAddRef(writer, sourceVariable.Load(), metadataAccess);
			writer.WriteLine("{0} = reinterpret_cast<void*>({1});", destinationVariable, LoadHandleFieldFor(sourceVariable.Load()));
		}

		public override void WriteMarshalCleanupVariable(IGeneratedMethodCodeWriter writer, string variableName, IRuntimeMetadataAccess metadataAccess, string managedVariableName = null)
		{
			if (!string.IsNullOrEmpty(managedVariableName))
			{
				writer.WriteLine("if ({0})", SafeHandleReferenceIncrementedLocalBoolName(managedVariableName));
				writer.BeginBlock();
				writer.WriteLine("{0};", Emit.Call(metadataAccess.Method(_releaseMethod), new string[2]
				{
					managedVariableName,
					metadataAccess.HiddenMethodInfo(_releaseMethod)
				}));
				writer.EndBlock();
			}
		}

		public override void WriteMarshalCleanupOutVariable(IGeneratedMethodCodeWriter writer, string variableName, IRuntimeMetadataAccess metadataAccess, string managedVariableName = null)
		{
		}

		public override void WriteDeclareAndAllocateObject(IGeneratedCodeWriter writer, string unmarshaledVariableName, string marshaledVariableName, IRuntimeMetadataAccess metadataAccess)
		{
			TypeDefinition typeDefinition = _typeRef.Resolve();
			if (typeDefinition.IsAbstract)
			{
				writer.WriteStatement(Emit.RaiseManagedException($"il2cpp_codegen_get_marshal_directive_exception(\"A returned SafeHandle cannot be abstract, but this type is: '{typeDefinition.FullName}'.\")"));
			}
			CustomMarshalInfoWriter.EmitNewObject(writer, _typeRef, unmarshaledVariableName, marshaledVariableName, emitNullCheck: false, metadataAccess);
		}

		public override void WriteMarshalVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, ManagedMarshalValue destinationVariable, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, bool callConstructor, IRuntimeMetadataAccess metadataAccess)
		{
			if (forNativeWrapperOfManagedMethod)
			{
				writer.WriteStatement(Emit.RaiseManagedException("il2cpp_codegen_get_marshal_directive_exception(\"Cannot marshal a SafeHandle from unmanaged to managed.\")"));
				return;
			}
			CustomMarshalInfoWriter.EmitCallToConstructor(writer, _typeRef.Resolve(), _defaultConstructor, destinationVariable, metadataAccess);
			string text = destinationVariable.GetNiceName() + "_handle_temp";
			writer.WriteLine("{0} {1};", _context.Global.Services.Naming.ForVariable(_context.Global.Services.TypeProvider.SystemIntPtr), text);
			writer.WriteLine("{0} = ({1}){2};", text, "intptr_t", variableName);
			writer.WriteLine("({0})->{1}({2});", destinationVariable.Load(), _context.Global.Services.Naming.ForFieldSetter(GetSafeHandleHandleField()), text);
			if (safeHandleShouldEmitAddRef)
			{
				EmitCallToDangerousAddRef(writer, destinationVariable.Load(), metadataAccess);
			}
		}

		public override void WriteIncludesForMarshaling(IGeneratedMethodCodeWriter writer)
		{
		}

		private string LoadHandleFieldFor(string sourceVariable)
		{
			return $"({sourceVariable})->{_context.Global.Services.Naming.ForFieldGetter(GetSafeHandleHandleField())}()";
		}

		private FieldReference GetSafeHandleHandleField()
		{
			return _safeHandleTypeDefinition.Fields.Single((FieldDefinition f) => f.Name == "handle");
		}

		private string SafeHandleReferenceIncrementedLocalBoolName(string variableName)
		{
			return string.Format("{0}_{1}", "___safeHandle_reference_incremented_for", CleanVariableName(variableName));
		}

		private void EmitCallToDangerousAddRef(ICodeWriter writer, string variableName, IRuntimeMetadataAccess metadataAccess)
		{
			string text = SafeHandleReferenceIncrementedLocalBoolName(variableName);
			writer.WriteLine("bool {0} = false;", text);
			writer.WriteLine("{0};", Emit.Call(metadataAccess.Method(_addRefMethod), new string[3]
			{
				variableName,
				Emit.AddressOf(text),
				metadataAccess.HiddenMethodInfo(_addRefMethod)
			}));
		}

		public override void WriteMarshaledTypeForwardDeclaration(IGeneratedCodeWriter writer)
		{
		}
	}
}
