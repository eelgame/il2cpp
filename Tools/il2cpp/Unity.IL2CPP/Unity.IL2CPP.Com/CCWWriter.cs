using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Unity.Cecil.Awesome;
using Unity.Cecil.Awesome.Comparers;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Marshaling;
using Unity.IL2CPP.Marshaling.BodyWriters.NativeToManaged;
using Unity.IL2CPP.Metadata;
using Unity.IL2CPP.Naming;
using Unity.IL2CPP.WindowsRuntime;

namespace Unity.IL2CPP.Com
{
	public class CCWWriter : CCWWriterBase
	{
		private sealed class InterfaceMethodMapping
		{
			public readonly MethodReference InterfaceMethod;

			public MethodReference ManagedMethod;

			public InterfaceMethodMapping(MethodReference interfaceMethod, MethodReference managedMethod)
			{
				InterfaceMethod = interfaceMethod;
				ManagedMethod = managedMethod;
			}
		}

		private readonly string _typeName;

		private readonly List<TypeReference> _interfacesToImplement;

		private readonly List<InterfaceMethodMapping> _interfaceMethodMappings;

		private readonly TypeReference[] _allInteropInterfaces;

		private readonly TypeReference[] _interfacesToForwardToBaseClass;

		private readonly bool _hasBaseClass;

		private readonly bool _implementsAnyIInspectableInterfaces;

		private readonly List<MethodReference> _implementedIReferenceMethods;

		private readonly GenericInstanceType _ireferenceOfType;

		private readonly TypeReference _boxedType;

		protected override bool ImplementsAnyIInspectableInterfaces => _implementsAnyIInspectableInterfaces;

		protected override IEnumerable<TypeReference> AllImplementedInterfaces => _interfacesToImplement;

		protected override bool HasBaseClass => _hasBaseClass;

		protected override IList<TypeReference> InterfacesToForwardToBaseClass => _interfacesToForwardToBaseClass;

		public CCWWriter(SourceWritingContext context, TypeReference type)
			: base(context, type)
		{
			_typeName = _context.Global.Services.Naming.ForComCallableWrapperClass(type);
			_interfaceMethodMappings = new List<InterfaceMethodMapping>();
			_interfacesToImplement = new List<TypeReference>();
			_implementedIReferenceMethods = new List<MethodReference>();
			_hasBaseClass = !type.IsArray && type.Resolve().GetTypeHierarchy().Any((TypeDefinition t) => t.IsComOrWindowsRuntimeType(context));
			_allInteropInterfaces = type.GetInterfacesImplementedByComCallableWrapper(context).ToArray();
			VTable vTable = ((!type.IsArray) ? new VTableBuilder().VTableFor(context, type) : null);
			foreach (TypeReference item in GetInterfacesToPotentiallyImplement(_allInteropInterfaces, GetInterfacesToNotImplement(type)))
			{
				int value = 0;
				bool flag = type.IsArray || !vTable.InterfaceOffsets.TryGetValue(item, out value);
				bool flag2 = false;
				List<InterfaceMethodMapping> list = new List<InterfaceMethodMapping>();
				int num = 0;
				foreach (MethodReference item2 in from m in item.GetMethods()
					where m.HasThis && m.Resolve().IsVirtual
					select m)
				{
					if (!item2.IsStripped() && !flag)
					{
						MethodReference methodReference = vTable.Slots[value + num];
						list.Add(new InterfaceMethodMapping(item2, methodReference));
						num++;
						if (!methodReference.DeclaringType.Resolve().IsComOrWindowsRuntimeType(context))
						{
							flag2 = true;
						}
					}
					else
					{
						list.Add(new InterfaceMethodMapping(item2, null));
					}
				}
				if (!_hasBaseClass || flag2 || flag)
				{
					_interfacesToImplement.Add(item);
					_interfaceMethodMappings.AddRange(list);
				}
			}
			_ireferenceOfType = GetIReferenceInterface(_type, out _boxedType);
			if (_ireferenceOfType != null)
			{
				_interfacesToImplement.Add(_ireferenceOfType);
				_interfacesToImplement.Add(_context.Global.Services.TypeProvider.IPropertyValueType);
				MethodDefinition method = _ireferenceOfType.Resolve().Methods.Single((MethodDefinition m) => m.Name == "get_Value");
				_implementedIReferenceMethods.Add(TypeResolver.For(_ireferenceOfType).Resolve(method));
				foreach (MethodDefinition method2 in _context.Global.Services.TypeProvider.IPropertyValueType.Resolve().Methods)
				{
					_implementedIReferenceMethods.Add(method2);
				}
			}
			_interfacesToForwardToBaseClass = _allInteropInterfaces.Except(_interfacesToImplement, new TypeReferenceEqualityComparer()).ToArray();
			_implementsAnyIInspectableInterfaces = _interfacesToImplement.Any((TypeReference i) => i.Resolve().IsExposedToWindowsRuntime());
		}

		private GenericInstanceType GetIReferenceInterface(TypeReference type, out TypeReference boxedType)
		{
			boxedType = null;
			TypeReference typeReference;
			TypeDefinition type2;
			if (type is ArrayType arrayType)
			{
				TypeDefinition typeDefinition = arrayType.ElementType.Resolve();
				if (type.CanBoxToWindowsRuntime(_context))
				{
					typeReference = arrayType.ElementType;
					boxedType = arrayType;
				}
				else
				{
					if (typeDefinition.MetadataType != MetadataType.Class || !typeDefinition.IsWindowsRuntime)
					{
						return null;
					}
					typeReference = _context.Global.Services.TypeProvider.SystemObject;
					boxedType = new ArrayType(typeReference);
				}
				type2 = _context.Global.Services.TypeProvider.IReferenceArrayType.Resolve();
			}
			else
			{
				if (!type.CanBoxToWindowsRuntime(_context))
				{
					return null;
				}
				type2 = _context.Global.Services.TypeProvider.IReferenceType.Resolve();
				typeReference = (boxedType = type);
			}
			return new GenericInstanceType(type2)
			{
				GenericArguments = { typeReference }
			};
		}

		public override void Write(IGeneratedMethodCodeWriter writer)
		{
			writer.AddInclude("vm/CachedCCWBase.h");
			AddIncludes(writer);
			string baseTypeName = GetBaseTypeName();
			writer.WriteLine();
			writer.WriteCommentedLine("COM Callable Wrapper for " + _type.FullName);
			writer.WriteLine("struct " + _typeName + " IL2CPP_FINAL : " + baseTypeName);
			using (new BlockWriter(writer, semicolon: true))
			{
				writer.WriteLine("inline " + _typeName + "(RuntimeObject* obj) : il2cpp::vm::CachedCCWBase<" + _typeName + ">(obj) {}");
				WriteCommonInterfaceMethods(writer);
				foreach (InterfaceMethodMapping interfaceMethodMapping in _interfaceMethodMappings)
				{
					WriteImplementedMethodDefinition(writer, interfaceMethodMapping);
				}
				foreach (MethodReference implementedIReferenceMethod in _implementedIReferenceMethods)
				{
					WriteImplementedIReferenceMethodDefinition(writer, implementedIReferenceMethod);
				}
			}
			_context.Global.Collectors.Stats.RecordComCallableWrapper();
		}

		private void WriteImplementedMethodDefinition(IGeneratedMethodCodeWriter writer, InterfaceMethodMapping mapping)
		{
			MarshalType marshalType = ((!mapping.InterfaceMethod.DeclaringType.Resolve().IsExposedToWindowsRuntime()) ? MarshalType.COM : MarshalType.WindowsRuntime);
			MethodReference methodReference = mapping.ManagedMethod ?? mapping.InterfaceMethod;
			string signature = ComInterfaceWriter.GetSignature(_context, methodReference, mapping.InterfaceMethod, TypeResolver.For(methodReference.DeclaringType));
			bool preserveSig = mapping.InterfaceMethod.Resolve().IsPreserveSig;
			WriteMethodDefinition(writer, signature, mapping.ManagedMethod, mapping.InterfaceMethod, delegate(IGeneratedMethodCodeWriter bodyWriter, IRuntimeMetadataAccess metadataAccess)
			{
				if (mapping.InterfaceMethod.IsStripped())
				{
					if (_hasBaseClass)
					{
						ForwardCallToBaseClassMethod(mapping.InterfaceMethod, mapping.InterfaceMethod, bodyWriter, marshalType, preserveSig);
					}
					else
					{
						bodyWriter.WriteCommentedLine("Managed method has been stripped");
						bodyWriter.WriteLine("return IL2CPP_E_ILLEGAL_METHOD_CALL;");
						_context.Global.Collectors.Stats.RecordStrippedComCallableWrapperMethod();
					}
				}
				else if (mapping.ManagedMethod == null)
				{
					TypeResolver typeResolver = TypeResolver.For(mapping.InterfaceMethod.DeclaringType);
					string text = writer.Context.Global.Services.Naming.ForComCallableWrapperProjectedMethod(mapping.InterfaceMethod);
					string text2 = MethodSignatureWriter.FormatComMethodParameterList(_context, mapping.InterfaceMethod, mapping.InterfaceMethod, typeResolver, marshalType, includeTypeNames: false, preserveSig);
					bodyWriter.AddMethodForwardDeclaration(MethodSignatureWriter.FormatProjectedComCallableWrapperMethodDeclaration(_context, mapping.InterfaceMethod, typeResolver, marshalType));
					if (string.IsNullOrEmpty(text2))
					{
						bodyWriter.WriteLine("return " + text + "(GetManagedObjectInline());");
					}
					else
					{
						bodyWriter.WriteLine("return " + text + "(GetManagedObjectInline(), " + text2 + ");");
					}
				}
				else if (!mapping.ManagedMethod.Resolve().DeclaringType.IsComOrWindowsRuntimeType(_context))
				{
					new ComCallableWrapperMethodBodyWriter(_context, mapping.ManagedMethod, mapping.InterfaceMethod, marshalType).WriteMethodBody(bodyWriter, metadataAccess);
					_context.Global.Collectors.Stats.RecordImplementedComCallableWrapperMethod();
				}
				else
				{
					ForwardCallToBaseClassMethod(mapping.InterfaceMethod, mapping.ManagedMethod, bodyWriter, marshalType, preserveSig);
				}
			});
		}

		private void ForwardCallToBaseClassMethod(MethodReference interfaceMethod, MethodReference managedMethod, IGeneratedMethodCodeWriter bodyWriter, MarshalType marshalType, bool preserveSig)
		{
			string text = _context.Global.Services.Naming.ForVariable(_type);
			string text2 = _context.Global.Services.Naming.ForTypeNameOnly(interfaceMethod.DeclaringType);
			string text3 = _context.Global.Services.Naming.ForMethod(interfaceMethod);
			string text4 = MethodSignatureWriter.FormatComMethodParameterList(_context, managedMethod, interfaceMethod, TypeResolver.For(_type), marshalType, includeTypeNames: false, preserveSig);
			bodyWriter.WriteLine("return il2cpp_codegen_com_query_interface<" + text2 + ">((" + text + ")GetManagedObjectInline())->" + text3 + "(" + text4 + ");");
			_context.Global.Collectors.Stats.RecordForwardedToBaseClassComCallableWrapperMethod();
		}

		private void WriteImplementedIReferenceMethodDefinition(IGeneratedMethodCodeWriter writer, MethodReference method)
		{
			string signature = ComInterfaceWriter.GetSignature(_context, method, method, TypeResolver.For(_ireferenceOfType));
			WriteMethodDefinition(writer, signature, null, method, delegate(IGeneratedMethodCodeWriter bodyWriter, IRuntimeMetadataAccess metadataAccess)
			{
				new IReferenceComCallableWrapperMethodBodyWriter(writer.Context, method, _boxedType).WriteMethodBody(bodyWriter, metadataAccess);
			});
			_context.Global.Collectors.Stats.RecordImplementedComCallableWrapperMethod();
		}

		private void WriteMethodDefinition(IGeneratedMethodCodeWriter writer, string signature, MethodReference managedMethod, MethodReference interfaceMethod, Action<IGeneratedMethodCodeWriter, IRuntimeMetadataAccess> writeAction)
		{
			writer.WriteLine();
			writer.WriteMethodWithMetadataInitialization(signature, (managedMethod ?? interfaceMethod).FullName, writeAction, _context.Global.Services.Naming.ForMethod(interfaceMethod) + "_CCW_" + ((managedMethod != null) ? (_typeName + "_" + _context.Global.Services.Naming.ForMethod(managedMethod)) : _typeName), managedMethod ?? interfaceMethod);
		}

		private string GetBaseTypeName()
		{
			StringBuilder stringBuilder = new StringBuilder("il2cpp::vm::CachedCCWBase<");
			stringBuilder.Append(_typeName);
			stringBuilder.Append('>');
			foreach (TypeReference item in _interfacesToImplement)
			{
				stringBuilder.Append(", ");
				stringBuilder.Append(_context.Global.Services.Naming.ForTypeNameOnly(item));
			}
			return stringBuilder.ToString();
		}

		private HashSet<TypeReference> GetInterfacesToNotImplement(TypeReference type)
		{
			HashSet<TypeReference> hashSet = new HashSet<TypeReference>(new TypeReferenceEqualityComparer());
			if (type.IsArray)
			{
				return hashSet;
			}
			do
			{
				TypeDefinition typeDefinition = type.Resolve();
				TypeResolver typeResolver = TypeResolver.For(type);
				if (typeDefinition.IsComOrWindowsRuntimeType(_context))
				{
					foreach (InterfaceImplementation @interface in typeDefinition.Interfaces)
					{
						if (!@interface.CustomAttributes.Any((CustomAttribute ca) => ca.AttributeType.FullName == "Windows.Foundation.Metadata.OverridableAttribute"))
						{
							hashSet.Add(typeResolver.Resolve(@interface.InterfaceType));
						}
					}
				}
				type = typeResolver.Resolve(typeDefinition.BaseType);
			}
			while (type != null);
			return hashSet;
		}

		private static IEnumerable<TypeReference> GetInterfacesToPotentiallyImplement(IEnumerable<TypeReference> allInteropInterfaces, HashSet<TypeReference> interfacesToNotImplement)
		{
			foreach (TypeReference allInteropInterface in allInteropInterfaces)
			{
				if (!interfacesToNotImplement.Contains(allInteropInterface))
				{
					yield return allInteropInterface;
				}
			}
		}
	}
}
