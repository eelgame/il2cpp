using Mono.Cecil;

namespace Unity.IL2CPP.Contexts.Services
{
	public interface INamingService
	{
		string ForTypeNameOnly(TypeReference type);

		string ForMethodNameOnly(MethodReference method);

		string ForStringLiteralIdentifier(string literal);

		string ForRuntimeUniqueTypeNameOnly(TypeReference type);

		string ForRuntimeUniqueMethodNameOnly(MethodReference method);

		string ForRuntimeUniqueStringLiteralIdentifier(string literal);

		string Clean(string name);

		string ForCurrentCodeGenModuleVar();

		string ForMetadataGlobalVar(string name);

		string ForReloadMethodMetadataInitialized();

		string ForComTypeInterfaceFieldGetter(TypeReference interfaceType);

		string ForInteropInterfaceVariable(TypeReference interfaceType);
	}
}
