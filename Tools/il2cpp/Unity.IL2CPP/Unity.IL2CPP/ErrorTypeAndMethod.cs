using System;
using Mono.Cecil;
using Unity.IL2CPP.Contexts;

namespace Unity.IL2CPP
{
	public class ErrorTypeAndMethod
	{
		public const string NameOfErrorType = "_ErrorTypeForTestingOnly_";

		public const string NameOfErrorMethod = "_ErrorMethodForTestingOnly_";

		public const string NameOfWarningType = "_WarningTypeForTestingOnly_";

		public const string NameOfWarningMethod = "_WarningMethodForTestingOnly_";

		public static void ThrowIfIsErrorType(ReadOnlyContext context, TypeDefinition typeDefinition)
		{
			VerifyRequiredCodeGenOptions(context);
			if (typeDefinition.Name == "_ErrorTypeForTestingOnly_")
			{
				throw new NotSupportedException(FormatMessage("type", "_ErrorTypeForTestingOnly_"));
			}
			if (typeDefinition.Name == "_WarningTypeForTestingOnly_")
			{
				context.Global.Services.MessageLogger.LogWarning(FormatMessage("type", "_WarningTypeForTestingOnly_"));
			}
		}

		public static void ThrowIfIsErrorMethod(ReadOnlyContext context, MethodReference method)
		{
			VerifyRequiredCodeGenOptions(context);
			if (method.Name == "_ErrorMethodForTestingOnly_")
			{
				throw new NotSupportedException(FormatMessage("method", "_ErrorMethodForTestingOnly_"));
			}
			if (method.Name == "_WarningMethodForTestingOnly_")
			{
				context.Global.Services.MessageLogger.LogWarning(FormatMessage("method", "_WarningMethodForTestingOnly_"));
			}
		}

		private static string FormatMessage(string typeOrMethod, string name)
		{
			return $"The managed {typeOrMethod} {name} always causes this exception when (--enable-error-message-test) is passed to il2cpp.exe. This exception is used only to test the il2cpp error reporting code.";
		}

		private static void VerifyRequiredCodeGenOptions(ReadOnlyContext context)
		{
			if (!context.Global.Parameters.EnableErrorMessageTest)
			{
				throw new InvalidOperationException("This method should only be called when the EnableErrorMessageTest command line option for il2cpp is set.");
			}
		}
	}
}
