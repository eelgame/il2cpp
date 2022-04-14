using System;
using Mono.Cecil;
using Unity.Cecil.Awesome;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Metadata.RuntimeTypes;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP
{
	public sealed class DefaultRuntimeMetadataAccess : IRuntimeMetadataAccess
	{
		private readonly SourceWritingContext _context;

		private readonly MethodMetadataUsage _methodMetadataUsage;

		private readonly MethodUsage _methodUsage;

		private readonly TypeResolver _typeResolver;

		private int _initMetadataInline;

		private bool InitRuntimeDataInline => _initMetadataInline > 0;

		public DefaultRuntimeMetadataAccess(SourceWritingContext context, MethodReference methodReference, MethodMetadataUsage methodMetadataUsage, MethodUsage methodUsage)
		{
			_context = context;
			_methodMetadataUsage = methodMetadataUsage;
			_methodUsage = methodUsage;
			if (methodReference != null)
			{
				_typeResolver = new TypeResolver(methodReference.DeclaringType as GenericInstanceType, methodReference as GenericInstanceMethod);
			}
			else
			{
				_typeResolver = TypeResolver.Empty;
			}
		}

		public string StaticData(TypeReference type)
		{
			TypeReference type2 = _typeResolver.Resolve(type);
			IIl2CppRuntimeType il2CppRuntimeType = _context.Global.Collectors.Types.Add(type2);
			_methodMetadataUsage.AddTypeInfo(il2CppRuntimeType, InitRuntimeDataInline);
			return FormatRuntimeIdentifier((Func<IIl2CppRuntimeType, string>)_context.Global.Services.Naming.ForRuntimeTypeInfo, il2CppRuntimeType, "RuntimeClass");
		}

		public string TypeInfoFor(TypeReference type)
		{
			return UnresolvedTypeInfoFor(_typeResolver.Resolve(type));
		}

		public string UnresolvedTypeInfoFor(TypeReference type)
		{
			IIl2CppRuntimeType il2CppRuntimeType = _context.Global.Collectors.Types.Add(type);
			_methodMetadataUsage.AddTypeInfo(il2CppRuntimeType, InitRuntimeDataInline);
			return FormatRuntimeIdentifier((Func<IIl2CppRuntimeType, string>)_context.Global.Services.Naming.ForRuntimeTypeInfo, il2CppRuntimeType, "RuntimeClass");
		}

		public string Il2CppTypeFor(TypeReference type)
		{
			TypeReference type2 = _typeResolver.Resolve(type, resolveGenericParameters: false);
			IIl2CppRuntimeType il2CppRuntimeType = _context.Global.Collectors.Types.Add(type2);
			_methodMetadataUsage.AddIl2CppType(il2CppRuntimeType, InitRuntimeDataInline);
			return FormatRuntimeIdentifier((Func<IIl2CppRuntimeType, string>)_context.Global.Services.Naming.ForRuntimeIl2CppType, il2CppRuntimeType, "RuntimeType");
		}

		public string ArrayInfo(TypeReference elementType)
		{
			ArrayType type = new ArrayType(_typeResolver.Resolve(elementType));
			return UnresolvedTypeInfoFor(type);
		}

		public string MethodInfo(MethodReference method)
		{
			MethodReference methodReference = _typeResolver.Resolve(method);
			if (method.IsGenericInstance || method.DeclaringType.IsGenericInstance)
			{
				_context.Global.Collectors.GenericMethods.Add(_context, methodReference);
			}
			_methodMetadataUsage.AddInflatedMethod(methodReference, InitRuntimeDataInline);
			return FormatRuntimeIdentifier((Func<MethodReference, string>)_context.Global.Services.Naming.ForRuntimeMethodInfo, methodReference, "RuntimeMethod");
		}

		public string HiddenMethodInfo(MethodReference method)
		{
			MethodReference methodReference = _typeResolver.Resolve(method);
			if (method.IsGenericInstance || method.DeclaringType.IsGenericInstance)
			{
				_context.Global.Collectors.GenericMethods.Add(_context, methodReference);
				_methodMetadataUsage.AddInflatedMethod(methodReference, InitRuntimeDataInline);
				return FormatRuntimeIdentifier((Func<MethodReference, string>)_context.Global.Services.Naming.ForRuntimeMethodInfo, methodReference, "RuntimeMethod");
			}
			return "NULL";
		}

		public string Newobj(MethodReference ctor)
		{
			TypeReference type = _typeResolver.Resolve(ctor.DeclaringType);
			IIl2CppRuntimeType il2CppRuntimeType = _context.Global.Collectors.Types.Add(type);
			_methodMetadataUsage.AddTypeInfo(il2CppRuntimeType, InitRuntimeDataInline);
			return FormatRuntimeIdentifier((Func<IIl2CppRuntimeType, string>)_context.Global.Services.Naming.ForRuntimeTypeInfo, il2CppRuntimeType, "RuntimeClass");
		}

		public string Method(MethodReference method)
		{
			if (MethodVerifier.IsNonGenericMethodThatDoesntExist(method))
			{
				method = method.Resolve();
			}
			MethodReference method2 = _typeResolver.Resolve(method);
			_methodUsage.AddMethod(method2);
			if (method.ShouldInline(_context.Global.Parameters))
			{
				return _context.Global.Services.Naming.ForMethod(method2) + "_inline";
			}
			return _context.Global.Services.Naming.ForMethod(method2);
		}

		public string FieldInfo(FieldReference field)
		{
			FieldReference fieldReference = _typeResolver.Resolve(field);
			Il2CppRuntimeFieldReference il2CppRuntimeFieldReference = new Il2CppRuntimeFieldReference(fieldReference, _context.Global.Collectors.Types.Add(fieldReference.DeclaringType));
			_methodMetadataUsage.AddFieldInfo(il2CppRuntimeFieldReference, InitRuntimeDataInline);
			return FormatRuntimeIdentifier((Func<Il2CppRuntimeFieldReference, string>)_context.Global.Services.Naming.ForRuntimeFieldInfo, il2CppRuntimeFieldReference, "RuntimeField");
		}

		public string StringLiteral(string literal)
		{
			return StringLiteral(literal, default(MetadataToken), _context.Global.Services.TypeProvider.Corlib);
		}

		public string StringLiteral(string literal, MetadataToken token, AssemblyDefinition assemblyDefinition)
		{
			if (literal == null)
			{
				return "NULL";
			}
			_methodMetadataUsage.AddStringLiteral(literal, assemblyDefinition, token, InitRuntimeDataInline);
			return FormatRuntimeIdentifier(_context.Global.Services.Naming.ForRuntimeUniqueStringLiteralIdentifier, literal, "String_t");
		}

		private string FormatRuntimeIdentifier<T>(Func<T, string> formatter, T value, string typeName)
		{
			string text = formatter(value);
			if (InitRuntimeDataInline)
			{
				return "((" + typeName + "*)il2cpp_codegen_initialize_runtime_metadata_inline((uintptr_t*)&" + text + "))";
			}
			return text;
		}

		public bool NeedsBoxingForValueTypeThis(MethodReference method)
		{
			return false;
		}

		public void StartInitMetadataInline()
		{
			_initMetadataInline++;
		}

		public void EndInitMetadataInline()
		{
			_initMetadataInline--;
		}
	}
}
