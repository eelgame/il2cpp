using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Unity.Cecil.Awesome;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.Marshaling.MarshalInfoWriters
{
	internal sealed class KeyValuePairMarshalInfoWriter : MarshalableMarshalInfoWriter
	{
		private readonly DefaultMarshalInfoWriter _keyMarshalInfoWriter;

		private readonly DefaultMarshalInfoWriter _valueMarshalInfoWriter;

		private readonly TypeReference _iKeyValuePair;

		private readonly string _iKeyValuePairTypeName;

		private readonly MarshaledType[] _marshaledTypes;

		public sealed override MarshaledType[] MarshaledTypes => _marshaledTypes;

		public KeyValuePairMarshalInfoWriter(ReadOnlyContext context, GenericInstanceType type)
			: base(context, type)
		{
			_keyMarshalInfoWriter = MarshalDataCollector.MarshalInfoWriterFor(context, type.GenericArguments[0], MarshalType.WindowsRuntime, null, useUnicodeCharSet: false, forByReferenceType: false, forFieldMarshaling: true);
			_valueMarshalInfoWriter = MarshalDataCollector.MarshalInfoWriterFor(context, type.GenericArguments[1], MarshalType.WindowsRuntime, null, useUnicodeCharSet: false, forByReferenceType: false, forFieldMarshaling: true);
			_iKeyValuePair = context.Global.Services.WindowsRuntime.ProjectToWindowsRuntime(type);
			_iKeyValuePairTypeName = context.Global.Services.Naming.ForTypeNameOnly(_iKeyValuePair);
			_marshaledTypes = new MarshaledType[1]
			{
				new MarshaledType(_iKeyValuePairTypeName + "*", _iKeyValuePairTypeName + "*")
			};
		}

		public override void WriteIncludesForFieldDeclaration(IGeneratedCodeWriter writer)
		{
			WriteMarshaledTypeForwardDeclaration(writer);
		}

		public override void WriteMarshaledTypeForwardDeclaration(IGeneratedCodeWriter writer)
		{
			writer.AddForwardDeclaration($"struct {_iKeyValuePairTypeName}");
		}

		public override void WriteIncludesForMarshaling(IGeneratedMethodCodeWriter writer)
		{
			writer.AddIncludeForTypeDefinition(_typeRef);
			writer.AddIncludeForTypeDefinition(_iKeyValuePair);
			_keyMarshalInfoWriter.WriteIncludesForMarshaling(writer);
			_valueMarshalInfoWriter.WriteIncludesForMarshaling(writer);
		}

		public override void WriteMarshalVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, string destinationVariable, string managedVariableName, IRuntimeMetadataAccess metadataAccess)
		{
			writer.WriteStatement(_context.Global.Services.Naming.ForVariable(_typeRef) + " " + sourceVariable.GetNiceName() + "Copy = " + sourceVariable.Load() + ";");
			writer.WriteStatement(destinationVariable + " = il2cpp_codegen_com_get_or_create_ccw<" + _iKeyValuePairTypeName + ">(" + Emit.Box(_context, _typeRef, sourceVariable.GetNiceName() + "Copy", metadataAccess) + ")");
		}

		public sealed override void WriteMarshalVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, ManagedMarshalValue destinationVariable, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, bool callConstructor, IRuntimeMetadataAccess metadataAccess)
		{
			string text = _context.Global.Services.Naming.ForInteropHResultVariable();
			TypeDefinition typeDefinition = _typeRef.Resolve();
			TypeResolver typeResolver = TypeResolver.For(_typeRef);
			FieldReference field = typeResolver.Resolve(typeDefinition.Fields.Single((FieldDefinition f) => f.Name == "key"));
			FieldReference field2 = typeResolver.Resolve(typeDefinition.Fields.Single((FieldDefinition f) => f.Name == "value"));
			TypeDefinition typeDefinition2 = _iKeyValuePair.Resolve();
			TypeResolver typeResolver2 = TypeResolver.For(_iKeyValuePair);
			MethodReference method = typeResolver2.Resolve(typeDefinition2.Methods.Single((MethodDefinition m) => m.Name == "get_Key"));
			MethodReference method2 = typeResolver2.Resolve(typeDefinition2.Methods.Single((MethodDefinition m) => m.Name == "get_Value"));
			string text2 = CleanVariableName(variableName);
			string keyVariableName = text2 + "KeyNative";
			string valueVariableName = text2 + "ValueNative";
			string text3 = text2 + "Staging";
			writer.WriteStatement(Emit.NullCheck(variableName));
			writer.WriteLine();
			using (new BlockWriter(writer))
			{
				string text4 = text2 + "_imanagedObject";
				writer.WriteLine("Il2CppIManagedObjectHolder* " + text4 + " = NULL;");
				writer.WriteLine("il2cpp_hresult_t " + text + " = (" + variableName + ")->QueryInterface(Il2CppIManagedObjectHolder::IID, reinterpret_cast<void**>(&" + text4 + "));");
				writer.WriteLine("if (IL2CPP_HR_SUCCEEDED(" + text + "))");
				using (new BlockWriter(writer))
				{
					writer.WriteLine(destinationVariable.Store("*static_cast<" + _context.Global.Services.Naming.ForVariable(_typeRef) + "*>(UnBox(" + text4 + "->GetManagedObject(), " + metadataAccess.TypeInfoFor(_typeRef) + "))"));
					writer.WriteLine(text4 + "->Release();");
				}
				writer.WriteLine("else");
				using (new BlockWriter(writer))
				{
					writer.WriteLine(_context.Global.Services.Naming.ForVariable(_typeRef) + " " + text3 + ";");
					_keyMarshalInfoWriter.WriteNativeVariableDeclarationOfType(writer, keyVariableName);
					writer.WriteLine(text + " = (" + variableName + ")->" + _context.Global.Services.Naming.ForMethod(method) + "(&" + keyVariableName + ");");
					writer.WriteLine("il2cpp_codegen_com_raise_exception_if_failed(" + text + ", false);");
					writer.WriteLine();
					_keyMarshalInfoWriter.WriteMarshalVariableFromNative(writer, keyVariableName, new ManagedMarshalValue(_context, text3, field), methodParameters, safeHandleShouldEmitAddRef: false, forNativeWrapperOfManagedMethod: false, callConstructor, metadataAccess);
					writer.WriteIfNotEmpty(delegate(IGeneratedMethodCodeWriter bodyWriter)
					{
						bodyWriter.WriteLine();
					}, delegate(IGeneratedMethodCodeWriter bodyWriter)
					{
						_keyMarshalInfoWriter.WriteMarshalCleanupVariable(bodyWriter, keyVariableName, metadataAccess);
					}, null);
					writer.WriteLine();
					_valueMarshalInfoWriter.WriteNativeVariableDeclarationOfType(writer, valueVariableName);
					writer.WriteLine(text + " = (" + variableName + ")->" + _context.Global.Services.Naming.ForMethod(method2) + "(&" + valueVariableName + ");");
					writer.WriteLine("il2cpp_codegen_com_raise_exception_if_failed(" + text + ", false);");
					writer.WriteLine();
					_valueMarshalInfoWriter.WriteMarshalVariableFromNative(writer, valueVariableName, new ManagedMarshalValue(_context, text3, field2), methodParameters, safeHandleShouldEmitAddRef: false, forNativeWrapperOfManagedMethod: false, callConstructor, metadataAccess);
					writer.WriteIfNotEmpty(delegate(IGeneratedMethodCodeWriter bodyWriter)
					{
						bodyWriter.WriteLine();
					}, delegate(IGeneratedMethodCodeWriter bodyWriter)
					{
						_valueMarshalInfoWriter.WriteMarshalCleanupVariable(bodyWriter, valueVariableName, metadataAccess);
					}, null);
					writer.WriteLine();
					writer.WriteLine(destinationVariable.Store(text3));
				}
			}
		}

		public sealed override void WriteMarshalCleanupVariable(IGeneratedMethodCodeWriter writer, string variableName, IRuntimeMetadataAccess metadataAccess, string managedVariableName)
		{
			writer.WriteLine("if ({0} != {1})", variableName, "NULL");
			using (new BlockWriter(writer))
			{
				writer.WriteLine("({0})->Release();", variableName);
				writer.WriteLine("{0} = {1};", variableName, "NULL");
			}
		}

		public override bool CanMarshalTypeFromNative()
		{
			if (_keyMarshalInfoWriter.CanMarshalTypeFromNative())
			{
				return _valueMarshalInfoWriter.CanMarshalTypeFromNative();
			}
			return false;
		}

		public override string GetMarshalingException()
		{
			if (!_keyMarshalInfoWriter.CanMarshalTypeFromNative())
			{
				return _keyMarshalInfoWriter.GetMarshalingException();
			}
			return _valueMarshalInfoWriter.GetMarshalingException();
		}
	}
}
