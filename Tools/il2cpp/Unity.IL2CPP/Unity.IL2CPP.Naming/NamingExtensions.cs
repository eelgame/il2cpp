using System.Collections.Generic;
using System.IO;
using Mono.Cecil;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.Metadata.RuntimeTypes;

namespace Unity.IL2CPP.Naming
{
	public static class NamingExtensions
	{
		public static string ForAssembly(this INamingService naming, AssemblyDefinition assembly)
		{
			return naming.Clean(assembly.Name.Name);
		}

		public static string ForCleanAssemblyFileName(this INamingService naming, AssemblyDefinition assembly)
		{
			return naming.Clean(Path.GetFileNameWithoutExtension(assembly.MainModule.FileName ?? assembly.MainModule.Name));
		}

		public static string ForThreadFieldsStruct(this INamingService naming, TypeReference type)
		{
			return naming.TypeMember(type, "ThreadStaticFields");
		}

		internal static string ForMethodInfoInternal(INamingService naming, MethodReference method, string suffix)
		{
			return naming.ForMethodNameOnly(method) + "_" + suffix;
		}

		internal static string ForRuntimeMethodInfoInternal(INamingService naming, MethodReference method, string suffix)
		{
			return naming.ForRuntimeUniqueMethodNameOnly(method) + "_" + suffix;
		}

		public static string ForGenericInst(this INamingService naming, IList<IIl2CppRuntimeType> types)
		{
			string text = naming.ForMetadataGlobalVar("GenInst");
			for (int i = 0; i < types.Count; i++)
			{
				text = text + "_" + naming.ForTypeNameOnly(types[i].Type);
			}
			return text;
		}

		public static string ForGenericClass(this INamingService naming, TypeReference type)
		{
			return naming.TypeMember(type, "GenericClass");
		}

		public static string ForStaticFieldsStruct(this INamingService naming, TypeReference type)
		{
			return naming.TypeMember(type, "StaticFields");
		}

		public static string ForStaticFieldsStructStorage(this INamingService naming, TypeReference type)
		{
			return naming.TypeMember(type, "StaticFields") + "_Storage";
		}

		public static string ForStaticFieldsRVAStructStorage(this INamingService naming, FieldReference field)
		{
			return naming.TypeMember(field.DeclaringType, "StaticFields") + "_" + naming.ForField(field) + "_RVAStorage";
		}

		public static string ForRuntimeIl2CppType(this INamingService naming, IIl2CppRuntimeType type)
		{
			return naming.ForIl2CppType(type) + "_var";
		}

		public static string ForRuntimeTypeInfo(this INamingService naming, IIl2CppRuntimeType type)
		{
			return naming.ForTypeInfo(type.Type) + "_var";
		}

		public static string ForRuntimeMethodInfo(this INamingService naming, MethodReference method)
		{
			return ForRuntimeMethodInfoInternal(naming, method, "RuntimeMethod") + "_var";
		}

		public static string ForRuntimeFieldInfo(this INamingService naming, Il2CppRuntimeFieldReference il2CppRuntimeField)
		{
			return naming.ForFieldInfo(il2CppRuntimeField.Field) + "_var";
		}

		public static string ForPadding(this INamingService naming, TypeDefinition typeDefinition)
		{
			return naming.ForType(typeDefinition) + "__padding";
		}

		public static string ForComTypeInterfaceFieldName(this INamingService naming, TypeReference interfaceType)
		{
			return naming.ForInteropInterfaceVariable(interfaceType);
		}

		public static string ForInteropHResultVariable(this INamingService naming)
		{
			return "hr";
		}

		public static string ForInteropReturnValue(this INamingService naming)
		{
			return "returnValue";
		}

		public static string ForComInterfaceReturnParameterName(this INamingService naming)
		{
			return "comReturnValue";
		}

		public static string ForPInvokeFunctionPointerTypedef(this INamingService naming)
		{
			return "PInvokeFunc";
		}

		public static string ForPInvokeFunctionPointerVariable(this INamingService naming)
		{
			return "il2cppPInvokeFunc";
		}

		public static string ForDelegatePInvokeWrapper(this INamingService naming, TypeReference type)
		{
			return "DelegatePInvokeWrapper_" + naming.ForType(type);
		}

		public static string ForReversePInvokeWrapperMethod(this INamingService naming, MethodReference method)
		{
			return naming.ForMetadataGlobalVar("ReversePInvokeWrapper_") + naming.ForMethodNameOnly(method);
		}

		public static string ForIl2CppComObjectIdentityField(this INamingService naming)
		{
			return "identity";
		}

		public static string ForMethodExecutionContextVariable(this INamingService naming)
		{
			return "methodExecutionContext";
		}

		public static string ForMethodExecutionContextThisVariable(this INamingService naming)
		{
			return "methodExecutionContextThis";
		}

		public static string ForMethodExecutionContextParametersVariable(this INamingService naming)
		{
			return "methodExecutionContextParameters";
		}

		public static string ForMethodExecutionContextLocalsVariable(this INamingService naming)
		{
			return "methodExecutionContextLocals";
		}

		public static string ForMethodNextSequencePointStorageVariable(this INamingService naming)
		{
			return "nextSequencePoint";
		}

		public static string ForMethodExitSequencePointChecker(this INamingService naming)
		{
			return "methodExitChecker";
		}

		private static string ForTypeInfo(this INamingService naming, TypeReference typeReference)
		{
			return naming.TypeMember(typeReference, "il2cpp_TypeInfo");
		}

		private static string ForFieldInfo(this INamingService naming, FieldReference field)
		{
			return naming.TypeMember(field.DeclaringType, naming.ForField(field) + "_FieldInfo");
		}

		public static string TypeMember(this INamingService naming, TypeReference type, string memberName)
		{
			string arg = (type.IsGenericParameter ? naming.ForGenericParameter((GenericParameter)type) : naming.ForRuntimeType(type));
			return $"{arg}_{memberName}";
		}

		public static string ForCodeGenModule(this INamingService naming, AssemblyDefinition assembly)
		{
			return "g_" + naming.ForCleanAssemblyFileName(assembly) + "_CodeGenModule";
		}

		public static string ForCurrentCodeGenModuleVarAddress(this INamingService naming)
		{
			string text = naming.ForCurrentCodeGenModuleVar();
			if (text != null)
			{
				return "&" + text;
			}
			return "NULL";
		}
	}
}
