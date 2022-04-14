using System.Collections.Generic;
using Mono.Cecil;
using Unity.Cecil.Awesome.Comparers;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Metadata.RuntimeTypes;

namespace Unity.IL2CPP
{
	public class CppDeclarations : CppDeclarationsBasic, ICppDeclarations, ICppDeclarationsBasic
	{
		public readonly HashSet<TypeReference> _typeIncludes = new HashSet<TypeReference>(new TypeReferenceEqualityComparer());

		public readonly HashSet<IIl2CppRuntimeType> _typeExterns = new HashSet<IIl2CppRuntimeType>(new Il2CppRuntimeTypeEqualityComparer());

		public readonly HashSet<IIl2CppRuntimeType[]> _genericInstExterns = new HashSet<IIl2CppRuntimeType[]>(new Il2CppRuntimeTypeArrayEqualityComparer());

		public readonly HashSet<TypeReference> _genericClassExterns = new HashSet<TypeReference>(new TypeReferenceEqualityComparer());

		public readonly HashSet<TypeReference> _forwardDeclarations = new HashSet<TypeReference>(new TypeReferenceEqualityComparer());

		public readonly HashSet<ArrayType> _arrayTypes = new HashSet<ArrayType>(new TypeReferenceEqualityComparer());

		public readonly HashSet<string> _rawFileLevelPreprocessorStmts = new HashSet<string>();

		public readonly HashSet<MethodReference> _methods = new HashSet<MethodReference>(new MethodReferenceComparer());

		public readonly HashSet<MethodReference> _sharedMethods = new HashSet<MethodReference>(new MethodReferenceComparer());

		public readonly HashSet<VirtualMethodDeclarationData> _virtualMethods = new HashSet<VirtualMethodDeclarationData>(new VirtualMethodDeclarationDataComparer());

		public readonly Dictionary<string, string> _internalPInvokeMethodDeclarations = new Dictionary<string, string>();

		public readonly Dictionary<string, string> _internalPInvokeMethodDeclarationsForForcedInternalPInvoke = new Dictionary<string, string>();

		public ReadOnlyHashSet<TypeReference> TypeIncludes => _typeIncludes.AsReadOnly();

		public ReadOnlyHashSet<IIl2CppRuntimeType> TypeExterns => _typeExterns.AsReadOnly();

		public ReadOnlyHashSet<IIl2CppRuntimeType[]> GenericInstExterns => _genericInstExterns.AsReadOnly();

		public ReadOnlyHashSet<TypeReference> GenericClassExterns => _genericClassExterns.AsReadOnly();

		public ReadOnlyHashSet<TypeReference> ForwardDeclarations => _forwardDeclarations.AsReadOnly();

		public ReadOnlyHashSet<ArrayType> ArrayTypes => _arrayTypes.AsReadOnly();

		public ReadOnlyHashSet<string> RawFileLevelPreprocessorStmts => _rawFileLevelPreprocessorStmts.AsReadOnly();

		public ReadOnlyHashSet<MethodReference> Methods => _methods.AsReadOnly();

		public ReadOnlyHashSet<MethodReference> SharedMethods => _sharedMethods.AsReadOnly();

		public ReadOnlyHashSet<VirtualMethodDeclarationData> VirtualMethods => _virtualMethods.AsReadOnly();

		public IReadOnlyDictionary<string, string> InternalPInvokeMethodDeclarations => _internalPInvokeMethodDeclarations.AsReadOnly();

		public IReadOnlyDictionary<string, string> InternalPInvokeMethodDeclarationsForForcedInternalPInvoke => _internalPInvokeMethodDeclarationsForForcedInternalPInvoke.AsReadOnly();

		public void Add(ICppDeclarations other)
		{
			_includes.UnionWith(other.Includes);
			_typeIncludes.UnionWith(other.TypeIncludes);
			_typeExterns.UnionWith(other.TypeExterns);
			_genericInstExterns.UnionWith(other.GenericInstExterns);
			_genericClassExterns.UnionWith(other.GenericClassExterns);
			_forwardDeclarations.UnionWith(other.ForwardDeclarations);
			_arrayTypes.UnionWith(other.ArrayTypes);
			_rawTypeForwardDeclarations.UnionWith(other.RawTypeForwardDeclarations);
			_rawMethodForwardDeclarations.UnionWith(other.RawMethodForwardDeclarations);
			_rawFileLevelPreprocessorStmts.UnionWith(other.RawFileLevelPreprocessorStmts);
			_methods.UnionWith(other.Methods);
			_sharedMethods.UnionWith(other.SharedMethods);
			_virtualMethods.UnionWith(other.VirtualMethods);
			foreach (string key in other.InternalPInvokeMethodDeclarations.Keys)
			{
				if (!_internalPInvokeMethodDeclarations.ContainsKey(key))
				{
					_internalPInvokeMethodDeclarations.Add(key, other.InternalPInvokeMethodDeclarations[key]);
				}
			}
			foreach (string key2 in other.InternalPInvokeMethodDeclarationsForForcedInternalPInvoke.Keys)
			{
				if (!_internalPInvokeMethodDeclarationsForForcedInternalPInvoke.ContainsKey(key2))
				{
					_internalPInvokeMethodDeclarationsForForcedInternalPInvoke.Add(key2, other.InternalPInvokeMethodDeclarationsForForcedInternalPInvoke[key2]);
				}
			}
		}
	}
}
