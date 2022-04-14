using Mono.Cecil;

namespace Unity.IL2CPP.Contexts.Services
{
	public interface ITypeProviderService
	{
		AssemblyDefinition Corlib { get; }

		TypeDefinition SystemObject { get; }

		TypeDefinition SystemString { get; }

		TypeDefinition SystemArray { get; }

		TypeDefinition SystemException { get; }

		TypeDefinition SystemDelegate { get; }

		TypeDefinition SystemMulticastDelegate { get; }

		TypeDefinition SystemByte { get; }

		TypeDefinition SystemUInt16 { get; }

		TypeDefinition SystemIntPtr { get; }

		TypeDefinition SystemUIntPtr { get; }

		TypeDefinition SystemVoid { get; }

		TypeDefinition SystemNullable { get; }

		TypeDefinition SystemType { get; }

		TypeDefinition TypedReference { get; }

		TypeReference Int32TypeReference { get; }

		TypeReference Int16TypeReference { get; }

		TypeReference UInt16TypeReference { get; }

		TypeReference SByteTypeReference { get; }

		TypeReference ByteTypeReference { get; }

		TypeReference BoolTypeReference { get; }

		TypeReference CharTypeReference { get; }

		TypeReference IntPtrTypeReference { get; }

		TypeReference UIntPtrTypeReference { get; }

		TypeReference Int64TypeReference { get; }

		TypeReference UInt32TypeReference { get; }

		TypeReference UInt64TypeReference { get; }

		TypeReference SingleTypeReference { get; }

		TypeReference DoubleTypeReference { get; }

		TypeReference ObjectTypeReference { get; }

		TypeReference StringTypeReference { get; }

		TypeReference RuntimeTypeHandleTypeReference { get; }

		TypeReference RuntimeMethodHandleTypeReference { get; }

		TypeReference RuntimeFieldHandleTypeReference { get; }

		TypeReference RuntimeArgumentHandleTypeReference { get; }

		TypeReference IActivationFactoryTypeReference { get; }

		TypeReference IIterableTypeReference { get; }

		TypeReference IBindableIterableTypeReference { get; }

		TypeReference IBindableIteratorTypeReference { get; }

		TypeReference IPropertyValueType { get; }

		TypeReference IReferenceType { get; }

		TypeReference IReferenceArrayType { get; }

		TypeDefinition IStringableType { get; }

		TypeReference Il2CppComObjectTypeReference { get; }

		TypeReference Il2CppComDelegateTypeReference { get; }

		TypeDefinition ConstantSplittableMapType { get; }

		TypeDefinition OptionalResolve(string namespaze, string name, AssemblyNameReference assembly);

		TypeDefinition OptionalResolveInCoreLibrary(string @namespace, string name);

		TypeReference GetSharedEnumType(TypeReference enumType);
	}
}
