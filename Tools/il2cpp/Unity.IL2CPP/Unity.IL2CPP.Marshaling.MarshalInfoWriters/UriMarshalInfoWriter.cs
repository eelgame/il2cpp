using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.Marshaling.MarshalInfoWriters
{
	internal sealed class UriMarshalInfoWriter : MarshalableMarshalInfoWriter
	{
		private readonly MarshaledType[] _marshaledTypes;

		private readonly TypeDefinition _windowsFoundationUri;

		private readonly TypeDefinition _iUriInterface;

		private readonly string _iUriInterfaceTypeName;

		public sealed override MarshaledType[] MarshaledTypes => _marshaledTypes;

		public UriMarshalInfoWriter(ReadOnlyContext context, TypeDefinition type)
			: base(context, type)
		{
			_windowsFoundationUri = context.Global.Services.WindowsRuntime.ProjectToWindowsRuntime(type);
			_iUriInterface = _windowsFoundationUri.ExtractDefaultInterface().Resolve();
			_iUriInterfaceTypeName = context.Global.Services.Naming.ForTypeNameOnly(_iUriInterface);
			string text = _iUriInterfaceTypeName + "*";
			_marshaledTypes = new MarshaledType[1]
			{
				new MarshaledType(text, text)
			};
		}

		public override void WriteMarshaledTypeForwardDeclaration(IGeneratedCodeWriter writer)
		{
			writer.AddForwardDeclaration("struct " + _iUriInterfaceTypeName + ";");
		}

		public override void WriteIncludesForMarshaling(IGeneratedMethodCodeWriter writer)
		{
			writer.AddIncludeForTypeDefinition(_typeRef);
			writer.AddIncludeForTypeDefinition(_windowsFoundationUri);
			writer.AddIncludeForTypeDefinition(_iUriInterface);
		}

		public override void WriteMarshalVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, string destinationVariable, string managedVariableName, IRuntimeMetadataAccess metadataAccess)
		{
			SourceWritingContext context = writer.Context;
			writer.WriteLine("if (" + sourceVariable.Load() + " != NULL)");
			using (new BlockWriter(writer))
			{
				MethodDefinition methodDefinition = _typeRef.Resolve().Methods.Single((MethodDefinition m) => m.Name == "get_OriginalString" && m.Parameters.Count == 0);
				string text = sourceVariable.GetNiceName() + "AsString";
				writer.WriteLine(context.Global.Services.Naming.ForVariable(context.Global.Services.TypeProvider.SystemString) + " " + text + ";");
				writer.WriteMethodCallWithResultStatement(metadataAccess, sourceVariable.Load(), methodDefinition, methodDefinition, MethodCallType.Normal, text);
				DefaultMarshalInfoWriter defaultMarshalInfoWriter = MarshalDataCollector.MarshalInfoWriterFor(context, context.Global.Services.TypeProvider.SystemString, MarshalType.WindowsRuntime);
				string text2 = defaultMarshalInfoWriter.WriteMarshalVariableToNative(writer, new ManagedMarshalValue(_context, text), text, metadataAccess);
				MethodDefinition nativeUriFactoryMethod = GetNativeUriFactoryMethod();
				string text3 = "((" + _context.Global.Services.Naming.ForStaticFieldsStruct(_windowsFoundationUri) + "*)" + metadataAccess.TypeInfoFor(_windowsFoundationUri) + "->static_fields)";
				string text4 = context.Global.Services.Naming.ForComTypeInterfaceFieldGetter(nativeUriFactoryMethod.DeclaringType);
				string text5 = context.Global.Services.Naming.ForMethod(nativeUriFactoryMethod);
				string text6 = context.Global.Services.Naming.ForInteropHResultVariable();
				writer.WriteLine("il2cpp_hresult_t " + text6 + " = " + text3 + "->" + text4 + "()->" + text5 + "(" + text2 + ", " + Emit.AddressOf(destinationVariable) + ");");
				defaultMarshalInfoWriter.WriteMarshalCleanupVariable(writer, text2, metadataAccess);
				writer.WriteLine("il2cpp_codegen_com_raise_exception_if_failed(" + text6 + ", false);");
			}
			writer.WriteLine("else");
			using (new BlockWriter(writer))
			{
				writer.WriteLine(destinationVariable + " = NULL;");
			}
		}

		private MethodDefinition GetNativeUriFactoryMethod()
		{
			TypeReference[] array = _windowsFoundationUri.GetActivationFactoryTypes(_context).ToArray();
			for (int i = 0; i < array.Length; i++)
			{
				foreach (MethodDefinition method in array[i].Resolve().Methods)
				{
					if (method.Parameters.Count == 1 && method.Parameters[0].ParameterType.MetadataType == MetadataType.String)
					{
						return method;
					}
				}
			}
			throw new InvalidProgramException("Could not find factory method to create Windows.Foundation.Uri object!");
		}

		public sealed override void WriteMarshalVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, ManagedMarshalValue destinationVariable, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, bool callConstructor, IRuntimeMetadataAccess metadataAccess)
		{
			writer.WriteLine("if (" + variableName + " != NULL)");
			using (new BlockWriter(writer))
			{
				MethodDefinition method = _iUriInterface.Methods.Single((MethodDefinition m) => m.Name == "get_RawUri" && m.Parameters.Count == 0);
				DefaultMarshalInfoWriter defaultMarshalInfoWriter = MarshalDataCollector.MarshalInfoWriterFor(writer.Context, writer.Context.Global.Services.TypeProvider.SystemString, MarshalType.WindowsRuntime);
				ManagedMarshalValue variableName2 = new ManagedMarshalValue(_context, destinationVariable.GetNiceName() + "AsString");
				string text = defaultMarshalInfoWriter.WriteMarshalEmptyVariableToNative(writer, variableName2, methodParameters);
				string text2 = _context.Global.Services.Naming.ForInteropHResultVariable();
				writer.WriteLine("il2cpp_hresult_t " + text2 + " = (" + variableName + ")->" + _context.Global.Services.Naming.ForMethod(method) + "(" + Emit.AddressOf(text) + ");");
				writer.WriteLine("il2cpp_codegen_com_raise_exception_if_failed(" + text2 + ", false);");
				writer.WriteLine();
				variableName2 = new ManagedMarshalValue(_context, defaultMarshalInfoWriter.WriteMarshalVariableFromNative(writer, text, methodParameters, safeHandleShouldEmitAddRef: true, forNativeWrapperOfManagedMethod, metadataAccess));
				defaultMarshalInfoWriter.WriteMarshalCleanupOutVariable(writer, text, metadataAccess);
				writer.WriteLine();
				string text3 = destinationVariable.GetNiceName() + "Temp";
				MethodDefinition methodDefinition = _typeRef.Resolve().Methods.Single((MethodDefinition m) => m.IsConstructor && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.MetadataType == MetadataType.String);
				writer.WriteLine(_context.Global.Services.Naming.ForVariable(_typeRef) + " " + text3 + " = " + Emit.NewObj(_context, _typeRef, metadataAccess) + ";");
				writer.WriteMethodCallStatement(metadataAccess, text3, methodDefinition, methodDefinition, MethodCallType.Normal, variableName2.Load());
				writer.WriteLine(destinationVariable.Store(text3));
			}
			writer.WriteLine("else");
			using (new BlockWriter(writer))
			{
				writer.WriteLine(destinationVariable.Store("NULL"));
			}
		}

		public sealed override void WriteMarshalCleanupVariable(IGeneratedMethodCodeWriter writer, string variableName, IRuntimeMetadataAccess metadataAccess, string managedVariableName)
		{
			writer.WriteLine("if (" + variableName + " != NULL)");
			using (new BlockWriter(writer))
			{
				writer.WriteLine("({0})->Release();", variableName);
				writer.WriteLine("{0} = {1};", variableName, "NULL");
			}
		}
	}
}
