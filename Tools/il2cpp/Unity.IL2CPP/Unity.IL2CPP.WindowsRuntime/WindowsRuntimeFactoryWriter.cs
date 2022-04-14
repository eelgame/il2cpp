using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Unity.Cecil.Awesome;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.Marshaling.BodyWriters;
using Unity.IL2CPP.Marshaling.BodyWriters.NativeToManaged;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.WindowsRuntime
{
	internal class WindowsRuntimeFactoryWriter : CCWWriterBase
	{
		private readonly string _typeName;

		private readonly MethodDefinition _parameterlessConstructor;

		private readonly List<TypeReference> _interfacesToImplement = new List<TypeReference>();

		private readonly List<(MethodDefinition, MethodReference)> _methodMappings = new List<(MethodDefinition, MethodReference)>();

		protected override IEnumerable<TypeReference> AllImplementedInterfaces => _interfacesToImplement;

		protected override bool ImplementsAnyIInspectableInterfaces => true;

		protected override bool IsManagedObjectHolder => false;

		public WindowsRuntimeFactoryWriter(SourceWritingContext context, TypeDefinition type)
			: base(context, type)
		{
			_typeName = _context.Global.Services.Naming.ForWindowsRuntimeFactory(type);
			List<TypeReference> list = new List<TypeReference>();
			List<TypeReference> list2 = new List<TypeReference>();
			foreach (CustomAttribute customAttribute in type.CustomAttributes)
			{
				TypeReference attributeType = customAttribute.AttributeType;
				if (!(attributeType.Namespace != "Windows.Foundation.Metadata") && customAttribute.ConstructorArguments.Count != 0 && customAttribute.ConstructorArguments[0].Value is TypeReference item)
				{
					if (attributeType.Name == "StaticAttribute")
					{
						list.Add(item);
					}
					else if (attributeType.Name == "ActivatableAttribute")
					{
						list2.Add(item);
					}
				}
			}
			_interfacesToImplement.Add(_context.Global.Services.TypeProvider.IActivationFactoryTypeReference);
			_interfacesToImplement.AddRange(list2);
			_interfacesToImplement.AddRange(list);
			foreach (MethodDefinition method in type.Methods)
			{
				if (!method.IsPublic)
				{
					continue;
				}
				MethodReference methodReference;
				if (method.IsConstructor)
				{
					if (method.Parameters.Count == 0)
					{
						_parameterlessConstructor = method;
						continue;
					}
					methodReference = method.GetFactoryMethodForConstructor(list2, isComposing: false);
				}
				else
				{
					if (method.HasThis)
					{
						continue;
					}
					methodReference = method.GetOverriddenInterfaceMethod(list);
				}
				if (methodReference != null)
				{
					_methodMappings.Add((method, methodReference));
				}
			}
		}

		public override void Write(IGeneratedMethodCodeWriter writer)
		{
			writer.AddInclude("vm/ActivationFactoryBase.h");
			AddIncludes(writer);
			string baseTypeName = GetBaseTypeName();
			writer.WriteLine();
			writer.WriteCommentedLine("Factory for " + _type.FullName);
			writer.WriteLine("struct " + _typeName + " IL2CPP_FINAL : " + baseTypeName);
			using (new BlockWriter(writer, semicolon: true))
			{
				if (_parameterlessConstructor != null)
				{
					WriteIActivationFactoryImplementation(writer);
				}
				foreach (var methodMapping in _methodMappings)
				{
					var (managedMethod, interfaceMethod) = methodMapping;
					writer.WriteCommentedLine("Native wrapper method for " + (managedMethod ?? interfaceMethod).FullName);
					string signature = ComInterfaceWriter.GetSignature(writer.Context, interfaceMethod, interfaceMethod, TypeResolver.Empty);
					string text = writer.Context.Global.Services.Naming.ForMethod(interfaceMethod);
					writer.WriteMethodWithMetadataInitialization(signature, text, delegate(IGeneratedMethodCodeWriter bodyWriter, IRuntimeMetadataAccess metadataAccess)
					{
						if (managedMethod != null)
						{
							GetMethodWriter(bodyWriter.Context, managedMethod, interfaceMethod).WriteMethodBody(bodyWriter, metadataAccess);
						}
						else
						{
							bodyWriter.WriteLine("return IL2CPP_E_NOTIMPL;");
						}
					}, text, interfaceMethod);
				}
				WriteCommonInterfaceMethods(writer);
			}
		}

		private void WriteIActivationFactoryImplementation(IGeneratedMethodCodeWriter writer)
		{
			INamingService naming = writer.Context.Global.Services.Naming;
			string methodSignature = "virtual il2cpp_hresult_t STDCALL ActivateInstance(Il2CppIInspectable** " + naming.ForComInterfaceReturnParameterName() + ") IL2CPP_OVERRIDE";
			writer.WriteMethodWithMetadataInitialization(methodSignature, "ActivateInstance", delegate(IGeneratedMethodCodeWriter bodyWriter, IRuntimeMetadataAccess metadataAccess)
			{
				TypeReference iActivationFactoryTypeReference = writer.Context.Global.Services.TypeProvider.IActivationFactoryTypeReference;
				GetMethodWriter(bodyWriter.Context, _parameterlessConstructor, iActivationFactoryTypeReference.Resolve().Methods.Single()).WriteMethodBody(bodyWriter, metadataAccess);
			}, _typeName + "_ActivateInstance", null);
		}

		private static InteropMethodBodyWriter GetMethodWriter(MinimalContext context, MethodDefinition managedMethod, MethodReference interfaceMethod)
		{
			if (managedMethod.IsConstructor)
			{
				return new ConstructorFactoryMethodBodyWriter(context, managedMethod, interfaceMethod);
			}
			return new StaticFactoryMethodBodyWriter(context, managedMethod, interfaceMethod);
		}

		private string GetBaseTypeName()
		{
			StringBuilder stringBuilder = new StringBuilder("il2cpp::vm::ActivationFactoryBase<");
			stringBuilder.Append(_typeName);
			stringBuilder.Append('>');
			foreach (TypeReference allImplementedInterface in AllImplementedInterfaces)
			{
				if (!allImplementedInterface.IsIActivationFactory(_context))
				{
					stringBuilder.Append(", ");
					stringBuilder.Append(_context.Global.Services.Naming.ForTypeNameOnly(allImplementedInterface));
				}
			}
			return stringBuilder.ToString();
		}

		public override void WriteCreateComCallableWrapperFunctionBody(IGeneratedMethodCodeWriter writer, IRuntimeMetadataAccess metadataAccess)
		{
			writer.WriteLine("return static_cast<Il2CppIActivationFactory*>(" + _typeName + "::__CreateInstance());");
		}
	}
}
