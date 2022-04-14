using Mono.Cecil;

namespace Unity.IL2CPP
{
	public interface IRuntimeMetadataAccess
	{
		string StaticData(TypeReference type);

		string TypeInfoFor(TypeReference type);

		string UnresolvedTypeInfoFor(TypeReference type);

		string ArrayInfo(TypeReference elementType);

		string Newobj(MethodReference ctor);

		string Il2CppTypeFor(TypeReference type);

		string Method(MethodReference method);

		string MethodInfo(MethodReference method);

		string HiddenMethodInfo(MethodReference method);

		string FieldInfo(FieldReference field);

		string StringLiteral(string literal);

		string StringLiteral(string literal, MetadataToken token, AssemblyDefinition assemblyDefinition);

		bool NeedsBoxingForValueTypeThis(MethodReference method);

		void StartInitMetadataInline();

		void EndInitMetadataInline();
	}
}
