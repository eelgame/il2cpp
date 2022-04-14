using Mono.Cecil;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.WindowsRuntime
{
	internal static class NamingExtensions
	{
		public static string ForCreateComCallableWrapperFunction(this INamingService naming, TypeReference type)
		{
			return "CreateComCallableWrapperFor_" + naming.ForTypeNameOnly(type);
		}

		public static string ForCreateWindowsRuntimeFactoryFunction(this INamingService naming, TypeDefinition type)
		{
			return "CreateWindowsRuntimeFactoryFor_" + naming.ForTypeNameOnly(type);
		}

		public static string ForComCallableWrapperClass(this INamingService naming, TypeReference type)
		{
			return naming.ForTypeNameOnly(type) + "_ComCallableWrapper";
		}

		public static string ForWindowsRuntimeFactory(this INamingService naming, TypeDefinition type)
		{
			return naming.ForTypeNameOnly(type) + "_Factory";
		}

		public static string ForComCallableWrapperProjectedMethod(this INamingService naming, MethodReference method)
		{
			return naming.ForMethodNameOnly(method) + "_ComCallableWrapperProjectedMethod";
		}

		public static string ForWindowsRuntimeAdapterClass(this INamingService naming, TypeReference type)
		{
			return naming.ForTypeNameOnly(type) + "_Adapter";
		}

		public static string ForWindowsRuntimeAdapterTypeName(this INamingService naming, TypeDefinition fromType, TypeDefinition toType)
		{
			string obj = ((fromType != null) ? RemoveBackticks(fromType.Name) : "IInspectable");
			string text = RemoveBackticks(toType.Name);
			string text2 = obj + "To" + text + "Adapter";
			if (toType.HasGenericParameters)
			{
				text2 = text2 + "`" + toType.GenericParameters.Count;
			}
			return text2;
		}

		public static string ForWindowsRuntimeDelegateNativeInvokerMethod(this INamingService naming, MethodReference invokeMethod)
		{
			return naming.ForMethod(invokeMethod) + "_NativeInvoker";
		}

		public static string ForWindowsRuntimeDelegateComCallableWrapperInterface(this INamingService naming, TypeReference delegateType)
		{
			return "I" + naming.ForComCallableWrapperClass(delegateType);
		}

		private static string RemoveBackticks(string typeName)
		{
			int num = typeName.IndexOf('`');
			if (num != -1)
			{
				typeName = typeName.Substring(0, num);
			}
			return typeName;
		}
	}
}
