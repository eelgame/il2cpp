using System;
using Mono.Cecil;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Scheduling.Streams;
using Unity.IL2CPP.Metadata.RuntimeTypes;

namespace Unity.IL2CPP.CodeWriters
{
	public interface IGeneratedCodeWriter : ICppCodeWriter, ICodeWriter, IDisposable, IStream
	{
		new SourceWritingContext Context { get; }

		ICppDeclarations Declarations { get; }

		void AddIncludesForTypeReference(TypeReference typeReference, bool requiresCompleteType = false);

		void AddForwardDeclaration(TypeReference type);

		void AddIncludeForTypeDefinition(TypeReference type);

		void AddIncludeOrExternForTypeDefinition(TypeReference type);

		void WriteExternForIl2CppType(IIl2CppRuntimeType type);

		void WriteExternForIl2CppGenericInst(IIl2CppRuntimeType[] type);

		void WriteExternForGenericClass(TypeReference type);

		void WriteVariable(TypeReference type, string name);

		void WriteDefaultReturn(TypeReference type);

		void Write(IGeneratedCodeWriter other);
	}
}
