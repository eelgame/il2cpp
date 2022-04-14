using System;
using System.Linq;
using Mono.Cecil;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.Marshaling.BodyWriters.ManagedToNative
{
	internal class WindowsRuntimeConstructorMethodBodyWriter : ManagedToNativeInteropMethodBodyWriter
	{
		private readonly TypeReference constructedObjectType;

		private readonly MethodReference factoryMethod;

		private readonly bool isComposingConstructor;

		private readonly string thisParameter = "__this";

		private readonly string identityField;

		public WindowsRuntimeConstructorMethodBodyWriter(MinimalContext context, MethodReference constructor)
			: base(context, constructor, constructor, MarshalType.WindowsRuntime, useUnicodeCharset: true)
		{
			constructedObjectType = constructor.DeclaringType;
			identityField = context.Global.Services.Naming.ForIl2CppComObjectIdentityField();
			TypeReference[] array = constructedObjectType.GetActivationFactoryTypes(context).ToArray();
			if (constructor.Parameters.Count != 0 || array.Length == 0)
			{
				factoryMethod = constructor.GetFactoryMethodForConstructor(array, isComposing: false);
				if (factoryMethod == null)
				{
					TypeReference[] activationFactoryTypes = constructedObjectType.GetComposableFactoryTypes().ToArray();
					factoryMethod = constructor.GetFactoryMethodForConstructor(activationFactoryTypes, isComposing: true);
					isComposingConstructor = true;
				}
				if (factoryMethod == null)
				{
					throw new InvalidOperationException(string.Format("Could not find factory method for Windows Runtime constructor " + constructor.FullName + "!"));
				}
			}
		}

		protected override void WriteInteropCallStatement(IGeneratedMethodCodeWriter writer, string[] localVariableNames, IRuntimeMetadataAccess metadataAccess)
		{
			string text = _context.Global.Services.Naming.ForStaticFieldsStruct(constructedObjectType);
			string text2 = metadataAccess.TypeInfoFor(constructedObjectType);
			string staticFieldsAccess = string.Format("((" + text + "*)" + text2 + "->static_fields)");
			string text3 = GetFunctionCallParametersExpression(localVariableNames, includesRetVal: false);
			if (text3.Length > 0)
			{
				text3 += ", ";
			}
			if (factoryMethod == null)
			{
				WriteActivateThroughIActivationFactory(writer, staticFieldsAccess, text3);
			}
			else if (!isComposingConstructor)
			{
				ActivateThroughCustomActivationFactory(writer, staticFieldsAccess, text3);
			}
			else
			{
				ActivateThroughCompositionFactory(writer, staticFieldsAccess, text3, metadataAccess);
			}
		}

		private void WriteActivateThroughIActivationFactory(IGeneratedMethodCodeWriter writer, string staticFieldsAccess, string parameters)
		{
			WriteDeclareActivationFactory(writer, _context.Global.Services.TypeProvider.IActivationFactoryTypeReference, staticFieldsAccess);
			writer.WriteLine("il2cpp_hresult_t hr = activationFactory->ActivateInstance(" + parameters + "reinterpret_cast<Il2CppIInspectable**>(&" + thisParameter + "->" + identityField + "));");
			writer.WriteLine("il2cpp_codegen_com_raise_exception_if_failed(hr, false);");
			writer.WriteLine();
			writer.WriteLine("il2cpp_codegen_com_register_rcw(" + thisParameter + ");");
		}

		private void ActivateThroughCustomActivationFactory(IGeneratedMethodCodeWriter writer, string staticFieldsAccess, string parameters)
		{
			string text = _context.Global.Services.Naming.ForMethod(factoryMethod);
			TypeReference typeReference = constructedObjectType.Resolve().ExtractDefaultInterface();
			string text2 = _context.Global.Services.Naming.ForTypeNameOnly(typeReference);
			string text3 = _context.Global.Services.Naming.ForComTypeInterfaceFieldName(typeReference);
			writer.AddIncludeForTypeDefinition(typeReference);
			WriteDeclareActivationFactory(writer, factoryMethod.DeclaringType, staticFieldsAccess);
			writer.WriteLine(text2 + "* " + text3 + ";");
			writer.WriteLine("il2cpp_hresult_t hr = activationFactory->" + text + "(" + parameters + "&" + text3 + ");");
			writer.WriteLine("il2cpp_codegen_com_raise_exception_if_failed(hr, false);");
			writer.WriteLine();
			writer.WriteLine("hr = " + text3 + "->QueryInterface(Il2CppIUnknown::IID, reinterpret_cast<void**>(&" + thisParameter + "->" + identityField + "));");
			writer.WriteLine("il2cpp_codegen_com_raise_exception_if_failed(hr, false);");
			writer.WriteLine();
			writer.WriteLine(thisParameter + "->qiShortCache[0].qiResult = " + text3 + ";");
			writer.WriteLine(thisParameter + "->qiShortCache[0].iid = &" + text2 + "::IID;");
			writer.WriteLine(thisParameter + "->qiShortCacheSize = 1;");
			writer.WriteLine("il2cpp_codegen_com_register_rcw(" + thisParameter + ");");
		}

		private void ActivateThroughCompositionFactory(IGeneratedMethodCodeWriter writer, string staticFieldsAccess, string parameters, IRuntimeMetadataAccess metadataAccess)
		{
			string text = metadataAccess.TypeInfoFor(constructedObjectType);
			string text2 = _context.Global.Services.Naming.ForMethod(factoryMethod);
			TypeReference typeReference = constructedObjectType.Resolve().ExtractDefaultInterface();
			string text3 = _context.Global.Services.Naming.ForTypeNameOnly(typeReference);
			string text4 = _context.Global.Services.Naming.ForComTypeInterfaceFieldName(typeReference);
			writer.AddIncludeForTypeDefinition(typeReference);
			writer.WriteLine("Il2CppIInspectable* outerInstance = NULL;");
			writer.WriteLine("Il2CppIInspectable** innerInstance = NULL;");
			writer.WriteLine("bool isComposedConstruction = " + thisParameter + "->klass != " + text + ";");
			WriteDeclareActivationFactory(writer, factoryMethod.DeclaringType, staticFieldsAccess);
			writer.WriteLine();
			writer.WriteLine("if (isComposedConstruction)");
			using (new BlockWriter(writer))
			{
				writer.WriteLine("outerInstance = il2cpp_codegen_com_get_or_create_ccw<Il2CppIInspectable>(" + thisParameter + ");");
				writer.WriteLine("innerInstance = reinterpret_cast<Il2CppIInspectable**>(&" + thisParameter + "->" + identityField + ");");
			}
			writer.WriteLine();
			writer.WriteLine(text3 + "* " + text4 + ";");
			writer.WriteLine("il2cpp_hresult_t hr = activationFactory->" + text2 + "(" + parameters + "outerInstance, innerInstance, &" + text4 + ");");
			writer.WriteLine("il2cpp_codegen_com_raise_exception_if_failed(hr, false);");
			writer.WriteLine();
			writer.WriteLine("if (isComposedConstruction)");
			using (new BlockWriter(writer))
			{
				writer.WriteLine("outerInstance->Release();");
				writer.WriteLine(text4 + "->Release();");
			}
			writer.WriteLine("else");
			using (new BlockWriter(writer))
			{
				writer.WriteLine("hr = " + text4 + "->QueryInterface(Il2CppIUnknown::IID, reinterpret_cast<void**>(&" + thisParameter + "->" + identityField + "));");
				writer.WriteLine("il2cpp_codegen_com_raise_exception_if_failed(hr, false);");
				writer.WriteLine();
				writer.WriteLine(thisParameter + "->qiShortCache[0].qiResult = " + text4 + ";");
				writer.WriteLine(thisParameter + "->qiShortCache[0].iid = &" + text3 + "::IID;");
				writer.WriteLine(thisParameter + "->qiShortCacheSize = 1;");
				writer.WriteLine("il2cpp_codegen_com_register_rcw(" + thisParameter + ");");
			}
		}

		private static void WriteDeclareActivationFactory(ICodeWriter writer, TypeReference factoryType, string staticFieldsAccess)
		{
			string text = writer.Context.Global.Services.Naming.ForTypeNameOnly(factoryType);
			string text2 = writer.Context.Global.Services.Naming.ForComTypeInterfaceFieldGetter(factoryType);
			writer.WriteLine(text + "* activationFactory = " + staticFieldsAccess + "->" + text2 + "();");
		}
	}
}
