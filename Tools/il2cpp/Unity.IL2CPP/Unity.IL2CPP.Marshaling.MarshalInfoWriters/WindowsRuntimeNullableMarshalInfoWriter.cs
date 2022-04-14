using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Unity.Cecil.Awesome;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.Marshaling.MarshalInfoWriters
{
	internal class WindowsRuntimeNullableMarshalInfoWriter : MarshalableMarshalInfoWriter
	{
		private readonly MarshaledType[] _marshaledTypes;

		private readonly GenericInstanceType _ireferenceInstance;

		private readonly TypeReference _boxedType;

		private readonly string _interfaceTypeName;

		private readonly DefaultMarshalInfoWriter _boxedTypeMarshalInfoWriter;

		public override MarshaledType[] MarshaledTypes => _marshaledTypes;

		public WindowsRuntimeNullableMarshalInfoWriter(ReadOnlyContext context, TypeReference type)
			: base(context, type)
		{
			_boxedType = ((GenericInstanceType)type).GenericArguments[0];
			_ireferenceInstance = new GenericInstanceType(context.Global.Services.TypeProvider.IReferenceType);
			_ireferenceInstance.GenericArguments.Add(_boxedType);
			_interfaceTypeName = context.Global.Services.Naming.ForTypeNameOnly(_ireferenceInstance);
			string text = _interfaceTypeName + "*";
			_marshaledTypes = new MarshaledType[1]
			{
				new MarshaledType(text, text)
			};
			_boxedTypeMarshalInfoWriter = MarshalDataCollector.MarshalInfoWriterFor(context, _boxedType, MarshalType.WindowsRuntime);
		}

		public override void WriteIncludesForMarshaling(IGeneratedMethodCodeWriter writer)
		{
			writer.AddIncludeForTypeDefinition(_typeRef);
			writer.AddIncludeForTypeDefinition(_ireferenceInstance);
			_boxedTypeMarshalInfoWriter.WriteIncludesForMarshaling(writer);
		}

		public override void WriteIncludesForFieldDeclaration(IGeneratedCodeWriter writer)
		{
			WriteMarshaledTypeForwardDeclaration(writer);
		}

		public override void WriteMarshaledTypeForwardDeclaration(IGeneratedCodeWriter writer)
		{
			writer.AddForwardDeclaration("struct " + writer.Context.Global.Services.Naming.ForTypeNameOnly(_ireferenceInstance));
		}

		private static FieldReference GetHasValueField(TypeDefinition nullableTypeDef, TypeResolver typeResolver)
		{
			return typeResolver.Resolve(nullableTypeDef.Fields.Single((FieldDefinition f) => !f.IsStatic && f.FieldType.MetadataType == MetadataType.Boolean));
		}

		private static FieldReference GetValueField(TypeDefinition nullableTypeDef, TypeResolver typeResolver)
		{
			return typeResolver.Resolve(nullableTypeDef.Fields.Single((FieldDefinition f) => !f.IsStatic && f.FieldType.MetadataType == MetadataType.Var));
		}

		public override void WriteMarshalVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, string destinationVariable, string managedVariableName, IRuntimeMetadataAccess metadataAccess)
		{
			SourceWritingContext context = writer.Context;
			TypeResolver typeResolver = TypeResolver.For(_typeRef);
			TypeDefinition nullableTypeDef = _typeRef.Resolve();
			string text = context.Global.Services.Naming.ForFieldGetter(GetHasValueField(nullableTypeDef, typeResolver));
			string text2 = context.Global.Services.Naming.ForFieldGetter(GetValueField(nullableTypeDef, typeResolver));
			string text3 = sourceVariable.GetNiceName() + "_value";
			string text4 = sourceVariable.GetNiceName() + "_boxed";
			writer.WriteLine("if (" + sourceVariable.Load() + "." + text + "())");
			using (new BlockWriter(writer))
			{
				writer.WriteLine(context.Global.Services.Naming.ForVariable(_boxedType) + " " + text3 + " = " + sourceVariable.Load() + "." + text2 + "();");
				writer.WriteLine(context.Global.Services.Naming.ForVariable(context.Global.Services.TypeProvider.SystemObject) + " " + text4 + " = Box(" + metadataAccess.TypeInfoFor(_boxedType) + ", &" + text3 + ");");
				writer.WriteLine(destinationVariable + " = il2cpp_codegen_com_get_or_create_ccw<" + _interfaceTypeName + ">(" + text4 + ");");
			}
			writer.WriteLine("else");
			using (new BlockWriter(writer))
			{
				writer.WriteLine(destinationVariable + " = NULL;");
			}
		}

		public override void WriteMarshalVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, ManagedMarshalValue destinationVariable, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, bool callConstructor, IRuntimeMetadataAccess metadataAccess)
		{
			SourceWritingContext context = writer.Context;
			TypeResolver typeResolver = TypeResolver.For(_typeRef);
			TypeDefinition nullableTypeDef = _typeRef.Resolve();
			string text = context.Global.Services.Naming.ForFieldSetter(GetHasValueField(nullableTypeDef, typeResolver));
			string text2 = context.Global.Services.Naming.ForFieldSetter(GetValueField(nullableTypeDef, typeResolver));
			MethodReference method = TypeResolver.For(_ireferenceInstance).Resolve(_context.Global.Services.TypeProvider.IReferenceType.Resolve().Methods.Single((MethodDefinition m) => m.Name == "get_Value"));
			string text3 = context.Global.Services.Naming.ForMethod(method);
			string text4 = destinationVariable.GetNiceName() + "_value_marshaled";
			string text5 = context.Global.Services.Naming.ForInteropHResultVariable();
			writer.WriteLine("if (" + variableName + " != NULL)");
			using (new BlockWriter(writer))
			{
				writer.WriteLine("Il2CppIManagedObjectHolder* imanagedObject = NULL;");
				writer.WriteLine("il2cpp_hresult_t " + text5 + " = (" + variableName + ")->QueryInterface(Il2CppIManagedObjectHolder::IID, reinterpret_cast<void**>(&imanagedObject));");
				writer.WriteLine("if (IL2CPP_HR_SUCCEEDED(" + text5 + "))");
				using (new BlockWriter(writer))
				{
					writer.WriteLine(destinationVariable.Load() + "." + text2 + "(*static_cast<" + context.Global.Services.Naming.ForVariable(_boxedType) + "*>(UnBox(imanagedObject->GetManagedObject())));");
					writer.WriteLine("imanagedObject->Release();");
				}
				writer.WriteLine("else");
				using (new BlockWriter(writer))
				{
					_boxedTypeMarshalInfoWriter.WriteNativeVariableDeclarationOfType(writer, text4);
					writer.WriteLine("hr = (" + variableName + ")->" + text3 + "(&" + text4 + ");");
					writer.WriteLine("il2cpp_codegen_com_raise_exception_if_failed(" + text5 + ", false);");
					writer.WriteLine();
					string text6 = _boxedTypeMarshalInfoWriter.WriteMarshalVariableFromNative(writer, text4, methodParameters, safeHandleShouldEmitAddRef: true, forNativeWrapperOfManagedMethod, metadataAccess);
					writer.WriteLine(destinationVariable.Load() + "." + text2 + "(" + text6 + ");");
				}
				writer.WriteLine();
				writer.WriteLine(destinationVariable.Load() + "." + text + "(true);");
			}
			writer.WriteLine("else");
			using (new BlockWriter(writer))
			{
				writer.WriteLine(destinationVariable.Load() + "." + text + "(false);");
			}
		}

		public override string WriteMarshalEmptyVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, IList<MarshaledParameter> methodParameters, IRuntimeMetadataAccess metadataAccess)
		{
			string text = "_" + CleanVariableName(variableName) + "_empty";
			writer.WriteVariable(_typeRef, text);
			return text;
		}

		public override void WriteMarshalCleanupVariable(IGeneratedMethodCodeWriter writer, string variableName, IRuntimeMetadataAccess metadataAccess, string managedVariableName = null)
		{
			writer.WriteLine("if (" + variableName + " != NULL)");
			using (new BlockWriter(writer))
			{
				writer.WriteLine("(" + variableName + ")->Release();");
			}
		}

		public override bool CanMarshalTypeToNative()
		{
			return _boxedTypeMarshalInfoWriter.CanMarshalTypeToNative();
		}

		public override bool CanMarshalTypeFromNative()
		{
			return _boxedTypeMarshalInfoWriter.CanMarshalTypeFromNative();
		}

		public override string GetMarshalingException()
		{
			return _boxedTypeMarshalInfoWriter.GetMarshalingException();
		}
	}
}
