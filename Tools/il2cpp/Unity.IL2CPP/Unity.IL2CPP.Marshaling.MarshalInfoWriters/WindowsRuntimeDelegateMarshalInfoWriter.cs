using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Unity.Cecil.Awesome;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Marshaling.BodyWriters.ManagedToNative;
using Unity.IL2CPP.Naming;
using Unity.IL2CPP.WindowsRuntime;

namespace Unity.IL2CPP.Marshaling.MarshalInfoWriters
{
	internal sealed class WindowsRuntimeDelegateMarshalInfoWriter : MarshalableMarshalInfoWriter
	{
		private readonly TypeResolver _typeResolver;

		private readonly MethodReference _invokeMethod;

		private readonly string _comCallableWrapperInterfaceName;

		private readonly string _nativeInvokerName;

		private readonly string _nativeInvokerSignature;

		private readonly MarshaledType[] _marshaledTypes;

		public override MarshaledType[] MarshaledTypes => _marshaledTypes;

		public WindowsRuntimeDelegateMarshalInfoWriter(ReadOnlyContext context, TypeReference type)
			: base(context, type)
		{
			TypeDefinition typeDefinition = type.Resolve();
			if (!typeDefinition.IsDelegate())
			{
				throw new ArgumentException($"WindowsRuntimeDelegateMarshalInfoWriter cannot marshal non-delegate type {type.FullName}.");
			}
			_typeResolver = TypeResolver.For(type);
			_invokeMethod = _typeResolver.Resolve(typeDefinition.Methods.Single((MethodDefinition m) => m.Name == "Invoke"));
			_comCallableWrapperInterfaceName = context.Global.Services.Naming.ForWindowsRuntimeDelegateComCallableWrapperInterface(type);
			_nativeInvokerName = context.Global.Services.Naming.ForWindowsRuntimeDelegateNativeInvokerMethod(_invokeMethod);
			_marshaledTypes = new MarshaledType[1]
			{
				new MarshaledType(_comCallableWrapperInterfaceName + "*", _comCallableWrapperInterfaceName + "*")
			};
			string returnType = MethodSignatureWriter.FormatReturnType(context, _typeResolver.Resolve(GenericParameterResolver.ResolveReturnTypeIfNeeded(_invokeMethod)));
			string parameters = string.Format("{0} {1}, {2}", context.Global.Services.Naming.ForVariable(context.Global.Services.TypeProvider.Il2CppComObjectTypeReference), "__this", MethodSignatureWriter.FormatParameters(context, _invokeMethod, ParameterFormat.WithTypeAndNameNoThis, includeHiddenMethodInfo: true));
			_nativeInvokerSignature = MethodSignatureWriter.GetMethodSignature(_nativeInvokerName, returnType, parameters, "IL2CPP_EXTERN_C", string.Empty);
		}

		public override void WriteMarshaledTypeForwardDeclaration(IGeneratedCodeWriter writer)
		{
			writer.AddForwardDeclaration("struct " + _comCallableWrapperInterfaceName);
		}

		public override void WriteNativeStructDefinition(IGeneratedCodeWriter writer)
		{
			foreach (ParameterDefinition parameter in _invokeMethod.Parameters)
			{
				MarshalDataCollector.MarshalInfoWriterFor(writer.Context, _typeResolver.Resolve(parameter.ParameterType), MarshalType.WindowsRuntime, null, useUnicodeCharSet: true).WriteMarshaledTypeForwardDeclaration(writer);
			}
			MarshalDataCollector.MarshalInfoWriterFor(writer.Context, _typeResolver.Resolve(_invokeMethod.ReturnType), MarshalType.WindowsRuntime, null, useUnicodeCharSet: true).WriteMarshaledTypeForwardDeclaration(writer);
			writer.WriteCommentedLine($"COM Callable Wrapper interface definition for {_typeRef.FullName}");
			writer.WriteLine("struct {0} : Il2CppIUnknown", _comCallableWrapperInterfaceName);
			using (new BlockWriter(writer, semicolon: true))
			{
				writer.WriteLine("static const Il2CppGuid IID;");
				string text = MethodSignatureWriter.FormatComMethodParameterList(_context, _invokeMethod, _invokeMethod, _typeResolver, MarshalType.WindowsRuntime, includeTypeNames: true, preserveSig: false);
				writer.WriteLine("virtual il2cpp_hresult_t STDCALL Invoke({0}) = 0;", text);
			}
			writer.WriteLine();
		}

		public override void WriteMarshalFunctionDeclarations(IGeneratedMethodCodeWriter writer)
		{
			writer.WriteStatement(_nativeInvokerSignature);
		}

		public override void WriteMarshalFunctionDefinitions(IGeneratedMethodCodeWriter writer)
		{
			writer.AddIncludeForTypeDefinition(_typeRef);
			writer.WriteLine("const Il2CppGuid {0}::IID = {1};", _comCallableWrapperInterfaceName, writer.Context.Global.Services.WindowsRuntime.ProjectToWindowsRuntime(_typeRef).GetGuid(_context).ToInitializer());
			writer.Context.Global.Collectors.InteropGuids.Add(writer.Context, _typeRef);
			WriteNativeInvoker(writer);
		}

		private void WriteNativeInvoker(IGeneratedMethodCodeWriter writer)
		{
			writer.WriteCommentedLine($"Native invoker for {_typeRef.FullName}");
			writer.WriteMethodWithMetadataInitialization(_nativeInvokerSignature, _invokeMethod.FullName, delegate(IGeneratedMethodCodeWriter bodyWriter, IRuntimeMetadataAccess metadataAccess)
			{
				new WindowsRuntimeDelegateMethodBodyWriter(writer.Context, _invokeMethod).WriteMethodBody(bodyWriter, metadataAccess);
			}, _nativeInvokerName, _invokeMethod);
		}

		public override void WriteNativeVariableDeclarationOfType(IGeneratedMethodCodeWriter writer, string variableName)
		{
			writer.WriteLine("{0}* {1} = {2};", _comCallableWrapperInterfaceName, variableName, "NULL");
		}

		public override string WriteMarshalEmptyVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue variableName, IList<MarshaledParameter> methodParameters)
		{
			return "NULL";
		}

		public override void WriteMarshalVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, string destinationVariable, string managedVariableName, IRuntimeMetadataAccess metadataAccess)
		{
			writer.WriteLine("if ({0} != {1})", sourceVariable.Load(), "NULL");
			using (new BlockWriter(writer))
			{
				FieldDefinition field = _context.Global.Services.TypeProvider.SystemDelegate.Fields.Single((FieldDefinition f) => f.Name == "m_target");
				string text = _context.Global.Services.Naming.ForFieldGetter(field);
				FieldDefinition field2 = _context.Global.Services.TypeProvider.SystemMulticastDelegate.Fields.Single((FieldDefinition f) => f.Name == "delegates");
				string text2 = _context.Global.Services.Naming.ForFieldGetter(field2);
				writer.WriteLine("RuntimeObject* target = {0}->{1}();", sourceVariable.Load(), text);
				writer.WriteLine();
				writer.WriteLine("if (target != {0} && {1}->{2}() == {0} && target->klass == {3})", "NULL", sourceVariable.Load(), text2, metadataAccess.TypeInfoFor(_context.Global.Services.TypeProvider.Il2CppComDelegateTypeReference));
				using (new BlockWriter(writer))
				{
					writer.WriteLine("il2cpp_hresult_t {0} = static_cast<{1}>(target)->{2}->QueryInterface({3}::IID, reinterpret_cast<void**>(&{4}));", _context.Global.Services.Naming.ForInteropHResultVariable(), _context.Global.Services.Naming.ForVariable(_context.Global.Services.TypeProvider.Il2CppComObjectTypeReference), _context.Global.Services.Naming.ForIl2CppComObjectIdentityField(), _comCallableWrapperInterfaceName, destinationVariable);
					writer.WriteStatement(Emit.Call("il2cpp_codegen_com_raise_exception_if_failed", _context.Global.Services.Naming.ForInteropHResultVariable(), "false"));
				}
				writer.WriteLine("else");
				using (new BlockWriter(writer))
				{
					writer.WriteLine("{0} = il2cpp_codegen_com_get_or_create_ccw<{1}>({2});", destinationVariable, _comCallableWrapperInterfaceName, sourceVariable.Load());
				}
			}
			writer.WriteLine("else");
			using (new BlockWriter(writer))
			{
				writer.WriteLine("{0} = {1};", destinationVariable, "NULL");
			}
		}

		public override void WriteMarshalVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, ManagedMarshalValue destinationVariable, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, bool callConstructor, IRuntimeMetadataAccess metadataAccess)
		{
			writer.WriteLine("if ({0} != {1})", variableName, "NULL");
			using (new BlockWriter(writer))
			{
				writer.WriteLine("Il2CppIManagedObjectHolder* imanagedObject = {0};", "NULL");
				writer.WriteLine("il2cpp_hresult_t {0} = ({1})->QueryInterface(Il2CppIManagedObjectHolder::IID, reinterpret_cast<void**>(&imanagedObject));", _context.Global.Services.Naming.ForInteropHResultVariable(), variableName);
				writer.WriteLine("if (IL2CPP_HR_SUCCEEDED({0}))", _context.Global.Services.Naming.ForInteropHResultVariable());
				using (new BlockWriter(writer))
				{
					writer.WriteLine(destinationVariable.Store("static_cast<{0}>(imanagedObject->GetManagedObject())", _context.Global.Services.Naming.ForVariable(_typeRef)));
					writer.WriteLine("imanagedObject->Release();");
				}
				writer.WriteLine("else");
				using (new BlockWriter(writer))
				{
					FieldDefinition field = _context.Global.Services.TypeProvider.SystemDelegate.Fields.Single((FieldDefinition f) => f.Name == "method_ptr");
					string text = _context.Global.Services.Naming.ForFieldSetter(field);
					FieldDefinition field2 = _context.Global.Services.TypeProvider.SystemDelegate.Fields.Single((FieldDefinition f) => f.Name == "method");
					string text2 = _context.Global.Services.Naming.ForFieldSetter(field2);
					FieldDefinition field3 = _context.Global.Services.TypeProvider.SystemDelegate.Fields.Single((FieldDefinition f) => f.Name == "m_target");
					string text3 = _context.Global.Services.Naming.ForFieldGetter(field3);
					string text4 = _context.Global.Services.Naming.ForFieldSetter(field3);
					writer.WriteLine(destinationVariable.Store(Emit.NewObj(_context, _typeRef, metadataAccess)));
					writer.AddMethodForwardDeclaration($"{_nativeInvokerSignature};");
					writer.WriteLine("{0}->{1}((Il2CppMethodPointer){2});", destinationVariable.Load(), text, _nativeInvokerName);
					writer.WriteLine("{0} methodInfo;", _context.Global.Services.Naming.ForVariable(_context.Global.Services.TypeProvider.SystemIntPtr));
					writer.WriteLine("methodInfo = reinterpret_cast<{0}>({1});", "intptr_t", metadataAccess.MethodInfo(_invokeMethod));
					writer.WriteLine("{0}->{1}(methodInfo);", destinationVariable.Load(), text2);
					writer.WriteLine("{0}->{1}(il2cpp_codegen_com_get_or_create_rcw_for_sealed_class<{2}>({3}, {4}));", destinationVariable.Load(), text4, _context.Global.Services.Naming.ForTypeNameOnly(_context.Global.Services.TypeProvider.Il2CppComDelegateTypeReference), variableName, metadataAccess.TypeInfoFor(_context.Global.Services.TypeProvider.Il2CppComDelegateTypeReference));
					writer.WriteLine("il2cpp_codegen_com_cache_queried_interface(static_cast<Il2CppComObject*>(" + destinationVariable.Load() + "->" + text3 + "()), " + _comCallableWrapperInterfaceName + "::IID, " + variableName + ");");
					writer.AddIncludeForTypeDefinition(_context.Global.Services.TypeProvider.Il2CppComDelegateTypeReference);
				}
			}
			writer.WriteLine("else");
			using (new BlockWriter(writer))
			{
				writer.WriteLine(destinationVariable.Store("NULL"));
			}
		}

		public override void WriteMarshalCleanupVariable(IGeneratedMethodCodeWriter writer, string variableName, IRuntimeMetadataAccess metadataAccess, string managedVariableName = null)
		{
			writer.WriteLine("if ({0} != {1})", variableName, "NULL");
			using (new BlockWriter(writer))
			{
				writer.WriteLine("({0})->Release();", variableName);
				writer.WriteLine("{0} = {1};", variableName, "NULL");
			}
		}
	}
}
