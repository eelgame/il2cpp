using System.Collections.Generic;
using Mono.Cecil;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Metadata.RuntimeTypes;

namespace Unity.IL2CPP
{
	public interface ICppDeclarations : ICppDeclarationsBasic
	{
		ReadOnlyHashSet<TypeReference> TypeIncludes { get; }

		ReadOnlyHashSet<IIl2CppRuntimeType> TypeExterns { get; }

		ReadOnlyHashSet<IIl2CppRuntimeType[]> GenericInstExterns { get; }

		ReadOnlyHashSet<TypeReference> GenericClassExterns { get; }

		ReadOnlyHashSet<TypeReference> ForwardDeclarations { get; }

		ReadOnlyHashSet<ArrayType> ArrayTypes { get; }

		ReadOnlyHashSet<string> RawFileLevelPreprocessorStmts { get; }

		ReadOnlyHashSet<MethodReference> Methods { get; }

		ReadOnlyHashSet<MethodReference> SharedMethods { get; }

		ReadOnlyHashSet<VirtualMethodDeclarationData> VirtualMethods { get; }

		IReadOnlyDictionary<string, string> InternalPInvokeMethodDeclarations { get; }

		IReadOnlyDictionary<string, string> InternalPInvokeMethodDeclarationsForForcedInternalPInvoke { get; }
	}
}
