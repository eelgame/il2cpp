using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace Unity.IL2CPP.ILPreProcessor
{
	public sealed class ApplyDefaultMarshalAsAttributeVisitor
	{
		public void Process(AssemblyDefinition assembly)
		{
			foreach (TypeDefinition allType in assembly.MainModule.GetAllTypes())
			{
				if (allType.IsWindowsRuntime)
				{
					break;
				}
				ProcessFields(allType);
				ProcessMethods(allType);
			}
		}

		private static void ProcessFields(TypeDefinition type)
		{
			if (type.IsPrimitive || type.IsEnum)
			{
				return;
			}
			foreach (FieldDefinition field in type.Fields)
			{
				ProcessObject(field.FieldType, field, NativeType.IUnknown);
				ProcessBoolean(field.FieldType, field, NativeType.Boolean);
			}
		}

		private static void ProcessMethods(TypeDefinition type)
		{
			bool isComInterface = type.IsComInterface();
			foreach (MethodReturnType item in type.Methods.Select((MethodDefinition m) => m.MethodReturnType))
			{
				if (isComInterface)
				{
					ProcessObject(item.ReturnType, item, NativeType.Struct);
				}
				ProcessBoolean(item.ReturnType, item, isComInterface ? NativeType.VariantBool : NativeType.Boolean);
			}
			foreach (ParameterDefinition item2 in type.Methods.Where((MethodDefinition m) => isComInterface || m.IsPInvokeImpl).SelectMany((MethodDefinition m) => m.Parameters))
			{
				ProcessObject(item2.ParameterType, item2, NativeType.Struct);
				ProcessBoolean(item2.ParameterType, item2, isComInterface ? NativeType.VariantBool : NativeType.Boolean);
			}
		}

		private static void ProcessObject(TypeReference type, IMarshalInfoProvider provider, NativeType nativeType)
		{
			switch (type.MetadataType)
			{
			case MetadataType.Object:
				if (!provider.HasMarshalInfo)
				{
					provider.MarshalInfo = new MarshalInfo(nativeType);
				}
				break;
			case MetadataType.ByReference:
				ProcessObject(((ByReferenceType)type).ElementType, provider, nativeType);
				break;
			case MetadataType.Array:
				if (((ArrayType)type).ElementType.MetadataType == MetadataType.Object && provider.MarshalInfo is ArrayMarshalInfo arrayMarshalInfo && (arrayMarshalInfo.ElementType == NativeType.None || arrayMarshalInfo.ElementType == NativeType.Max))
				{
					arrayMarshalInfo.ElementType = nativeType;
				}
				break;
			}
		}

		private static void ProcessBoolean(TypeReference type, IMarshalInfoProvider provider, NativeType nativeType)
		{
			switch (type.MetadataType)
			{
			case MetadataType.Boolean:
				if (!provider.HasMarshalInfo)
				{
					provider.MarshalInfo = new MarshalInfo(nativeType);
				}
				break;
			case MetadataType.ByReference:
				ProcessBoolean(((ByReferenceType)type).ElementType, provider, nativeType);
				break;
			case MetadataType.Array:
				if (((ArrayType)type).ElementType.MetadataType == MetadataType.Boolean && provider.MarshalInfo is ArrayMarshalInfo arrayMarshalInfo && (arrayMarshalInfo.ElementType == NativeType.None || arrayMarshalInfo.ElementType == NativeType.Max))
				{
					arrayMarshalInfo.ElementType = nativeType;
				}
				break;
			}
		}
	}
}
