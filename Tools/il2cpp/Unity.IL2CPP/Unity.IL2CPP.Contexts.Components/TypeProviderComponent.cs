using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Unity.Cecil.Awesome.Comparers;
using Unity.IL2CPP.Contexts.Components.Base;
using Unity.IL2CPP.Contexts.Services;

namespace Unity.IL2CPP.Contexts.Components
{
	public class TypeProviderComponent : ServiceComponentBase<ITypeProviderService, TypeProviderComponent>, ITypeProviderService
	{
		private AssemblyDefinition _mscorlib;

		private TypeReference _runtimeTypeHandleType;

		private TypeReference _runtimeMethodHandleType;

		private TypeReference _runtimeFieldHandleType;

		private TypeReference _runtimeArgumentHandleType;

		private TypeDefinition _iActivationFactoryType;

		private TypeDefinition _iPropertyValueType;

		private TypeDefinition _iReferenceType;

		private TypeDefinition _iReferenceArrayType;

		private TypeDefinition _iIterableType;

		private TypeDefinition _iBindableIterableType;

		private TypeDefinition _iBindableIteratorType;

		private TypeDefinition _il2cppComObjectType;

		private TypeDefinition _il2cppComDelegateType;

		private TypeDefinition _iStringableType;

		private TypeDefinition _constantSplittableMapType;

		private Dictionary<TypeReference, TypeReference> _sharedEnumTypes;

		public AssemblyDefinition Corlib => _mscorlib;

		public TypeDefinition SystemObject => _mscorlib.MainModule.GetType("System.Object");

		public TypeDefinition SystemString => _mscorlib.MainModule.GetType("System.String");

		public TypeDefinition SystemArray => _mscorlib.MainModule.GetType("System.Array");

		public TypeDefinition SystemException => _mscorlib.MainModule.GetType("System.Exception");

		public TypeDefinition SystemDelegate => _mscorlib.MainModule.GetType("System.Delegate");

		public TypeDefinition SystemMulticastDelegate => _mscorlib.MainModule.GetType("System.MulticastDelegate");

		public TypeDefinition SystemByte => _mscorlib.MainModule.GetType("System.Byte");

		public TypeDefinition SystemUInt16 => _mscorlib.MainModule.GetType("System.UInt16");

		public TypeDefinition SystemIntPtr => _mscorlib.MainModule.GetType("System.IntPtr");

		public TypeDefinition SystemUIntPtr => _mscorlib.MainModule.GetType("System.UIntPtr");

		public TypeDefinition SystemVoid => _mscorlib.MainModule.GetType("System.Void");

		public TypeDefinition SystemNullable => _mscorlib.MainModule.GetType("System.Nullable`1");

		public TypeDefinition SystemType => _mscorlib.MainModule.GetType("System.Type");

		public TypeDefinition TypedReference => _mscorlib.MainModule.GetType("System.TypedReference");

		public TypeReference Int32TypeReference => _mscorlib.MainModule.TypeSystem.Int32;

		public TypeReference Int16TypeReference => _mscorlib.MainModule.TypeSystem.Int16;

		public TypeReference UInt16TypeReference => _mscorlib.MainModule.TypeSystem.UInt16;

		public TypeReference SByteTypeReference => _mscorlib.MainModule.TypeSystem.SByte;

		public TypeReference ByteTypeReference => _mscorlib.MainModule.TypeSystem.Byte;

		public TypeReference BoolTypeReference => _mscorlib.MainModule.TypeSystem.Boolean;

		public TypeReference CharTypeReference => _mscorlib.MainModule.TypeSystem.Char;

		public TypeReference IntPtrTypeReference => _mscorlib.MainModule.TypeSystem.IntPtr;

		public TypeReference UIntPtrTypeReference => _mscorlib.MainModule.TypeSystem.UIntPtr;

		public TypeReference Int64TypeReference => _mscorlib.MainModule.TypeSystem.Int64;

		public TypeReference UInt32TypeReference => _mscorlib.MainModule.TypeSystem.UInt32;

		public TypeReference UInt64TypeReference => _mscorlib.MainModule.TypeSystem.UInt64;

		public TypeReference SingleTypeReference => _mscorlib.MainModule.TypeSystem.Single;

		public TypeReference DoubleTypeReference => _mscorlib.MainModule.TypeSystem.Double;

		public TypeReference ObjectTypeReference => _mscorlib.MainModule.TypeSystem.Object;

		public TypeReference StringTypeReference => _mscorlib.MainModule.TypeSystem.String;

		public TypeReference RuntimeTypeHandleTypeReference => _runtimeTypeHandleType;

		public TypeReference RuntimeMethodHandleTypeReference => _runtimeMethodHandleType;

		public TypeReference RuntimeFieldHandleTypeReference => _runtimeFieldHandleType;

		public TypeReference RuntimeArgumentHandleTypeReference => _runtimeArgumentHandleType;

		public TypeReference IActivationFactoryTypeReference => _iActivationFactoryType;

		public TypeReference IPropertyValueType => _iPropertyValueType;

		public TypeReference IReferenceType => _iReferenceType;

		public TypeReference IReferenceArrayType => _iReferenceArrayType;

		public TypeReference IIterableTypeReference => _iIterableType;

		public TypeReference IBindableIterableTypeReference => _iBindableIterableType;

		public TypeReference IBindableIteratorTypeReference => _iBindableIteratorType;

		public TypeReference Il2CppComObjectTypeReference => _il2cppComObjectType;

		public TypeReference Il2CppComDelegateTypeReference => _il2cppComDelegateType;

		public TypeDefinition IStringableType => _iStringableType;

		public TypeDefinition ConstantSplittableMapType => _constantSplittableMapType;

		public void Initialize(AssemblyConversionContext context)
		{
			AssemblyDefinition assemblyDefinition = context.Results.Initialize.AllAssembliesOrderedByDependency.FirstOrDefault();
			if (assemblyDefinition == null)
			{
				throw new InvalidOperationException("One or more assemblies must be setup for conversion");
			}
			_mscorlib = context.Services.AssemblyLoader.Resolve(assemblyDefinition.MainModule.TypeSystem.CoreLibrary);
			if (_mscorlib.MainModule.HasExportedTypes)
			{
				_mscorlib = context.Services.AssemblyLoader.Resolve(_mscorlib.MainModule.TypeSystem.CoreLibrary);
			}
			_sharedEnumTypes = new Dictionary<TypeReference, TypeReference>(new TypeReferenceEqualityComparer());
			_iActivationFactoryType = new TypeDefinition(string.Empty, "IActivationFactory", TypeAttributes.NotPublic, SystemObject);
			_iActivationFactoryType.IsInterface = true;
			_iActivationFactoryType.IsWindowsRuntime = true;
			MethodDefinition item = new MethodDefinition("ActivateInstance", MethodAttributes.Public | MethodAttributes.Virtual, SystemObject);
			_iActivationFactoryType.Methods.Add(item);
			if (!context.Parameters.UsingTinyBackend)
			{
				if (context.Parameters.CanShareEnumTypes)
				{
					AddSharedEnumTypes(_sharedEnumTypes);
					KeyValuePair<TypeReference, TypeReference>[] array = _sharedEnumTypes.Where((KeyValuePair<TypeReference, TypeReference> kvp) => kvp.Value == null).ToArray();
					if (array.Any())
					{
						throw new InvalidOperationException("One or more shared enum types could not be found.  Was the embedded mscorlib.xml file present when UnityLinker Ran?\nMissing types were\n" + array.AggregateWithNewLine());
					}
				}
				TypeAttributes attributes = TypeAttributes.BeforeFieldInit;
				_il2cppComObjectType = new TypeDefinition("System", "__Il2CppComObject", attributes, SystemObject);
				_mscorlib.MainModule.Types.Add(_il2cppComObjectType);
				if (context.InputData.Profile.SupportsWindowsRuntime)
				{
					TypeAttributes attributes2 = TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit;
					_il2cppComDelegateType = new TypeDefinition("System", "__Il2CppComDelegate", attributes2, _il2cppComObjectType);
					_mscorlib.MainModule.Types.Add(_il2cppComDelegateType);
				}
			}
			foreach (TypeDefinition type in _mscorlib.MainModule.Types)
			{
				if (type.Namespace == "System")
				{
					if (type.Name == "RuntimeTypeHandle")
					{
						_runtimeTypeHandleType = type;
					}
					else if (type.Name == "RuntimeMethodHandle")
					{
						_runtimeMethodHandleType = type;
					}
					else if (type.Name == "RuntimeFieldHandle")
					{
						_runtimeFieldHandleType = type;
					}
					else if (type.Name == "RuntimeArgumentHandle")
					{
						_runtimeArgumentHandleType = type;
					}
				}
			}
			AssemblyNameReference assembly = new AssemblyNameReference("Windows", new Version(255, 255, 255, 255))
			{
				IsWindowsRuntime = true
			};
			_iPropertyValueType = OptionalResolve("Windows.Foundation", "IPropertyValue", assembly);
			_iReferenceType = OptionalResolve("Windows.Foundation", "IReference`1", assembly);
			_iReferenceArrayType = OptionalResolve("Windows.Foundation", "IReferenceArray`1", assembly);
			if (_iPropertyValueType == null != (_iReferenceType == null) || _iPropertyValueType == null != (_iReferenceArrayType == null))
			{
				throw new InvalidProgramException("Windows.Foundation.IPropertyValue, Windows.Foundation.IReference`1<T> and Windows.Foundation.IReferenceArray`1<T> are a package deal. Either all or none must be available. Are stripper link.xml files configured correctly?");
			}
			_iIterableType = OptionalResolve("Windows.Foundation.Collections", "IIterable`1", assembly);
			_iBindableIterableType = OptionalResolve("Windows.UI.Xaml.Interop", "IBindableIterable", assembly);
			_iBindableIteratorType = OptionalResolve("Windows.UI.Xaml.Interop", "IBindableIterator", assembly);
			_iStringableType = OptionalResolve("Windows.Foundation", "IStringable", assembly);
			AssemblyNameReference assembly2 = new AssemblyNameReference("System.Runtime.WindowsRuntime", new Version(4, 0, 0, 0));
			_constantSplittableMapType = OptionalResolve("System.Runtime.InteropServices.WindowsRuntime", "ConstantSplittableMap`2", assembly2);
		}

		private void AddSharedEnumTypes(Dictionary<TypeReference, TypeReference> enumMap)
		{
			enumMap.Add(_mscorlib.MainModule.TypeSystem.SByte, _mscorlib.MainModule.GetType("System", "SByteEnum"));
			enumMap.Add(_mscorlib.MainModule.TypeSystem.Int16, _mscorlib.MainModule.GetType("System", "Int16Enum"));
			enumMap.Add(_mscorlib.MainModule.TypeSystem.Int32, _mscorlib.MainModule.GetType("System", "Int32Enum"));
			enumMap.Add(_mscorlib.MainModule.TypeSystem.Int64, _mscorlib.MainModule.GetType("System", "Int64Enum"));
			enumMap.Add(_mscorlib.MainModule.TypeSystem.Byte, _mscorlib.MainModule.GetType("System", "ByteEnum"));
			enumMap.Add(_mscorlib.MainModule.TypeSystem.UInt16, _mscorlib.MainModule.GetType("System", "UInt16Enum"));
			enumMap.Add(_mscorlib.MainModule.TypeSystem.UInt32, _mscorlib.MainModule.GetType("System", "UInt32Enum"));
			enumMap.Add(_mscorlib.MainModule.TypeSystem.UInt64, _mscorlib.MainModule.GetType("System", "UInt64Enum"));
		}

		public TypeDefinition OptionalResolveInCoreLibrary(string @namespace, string name)
		{
			return OptionalResolve(@namespace, name, _mscorlib.Name);
		}

		public TypeReference GetSharedEnumType(TypeReference enumType)
		{
			return _sharedEnumTypes[enumType.GetUnderlyingEnumType()];
		}

		public TypeDefinition OptionalResolve(string namespaze, string name, AssemblyNameReference assembly)
		{
			TypeReference typeReference = new TypeReference(namespaze, name, _mscorlib.MainModule, assembly);
			try
			{
				return typeReference.Resolve();
			}
			catch (AssemblyResolutionException)
			{
				return null;
			}
		}

		protected override TypeProviderComponent ThisAsFull()
		{
			return this;
		}

		protected override ITypeProviderService ThisAsRead()
		{
			return this;
		}
	}
}
