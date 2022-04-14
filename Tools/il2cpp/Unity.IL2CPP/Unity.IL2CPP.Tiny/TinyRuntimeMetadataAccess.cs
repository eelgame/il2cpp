using System;
using Mono.Cecil;
using Unity.Cecil.Awesome;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Metadata;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.Tiny
{
	public sealed class TinyRuntimeMetadataAccess : IRuntimeMetadataAccess
	{
		private readonly TinyWriteContext _context;

		private readonly MethodMetadataUsage _methodMetadataUsage;

		private readonly TypeResolver _typeResolver;

		private readonly MethodUsage _methodUsage;

		private bool _stringTypeCached;

		public TinyRuntimeMetadataAccess(TinyWriteContext context, MethodReference methodReference, MethodMetadataUsage methodMetadataUsage, MethodUsage methodUsage)
		{
			_context = context;
			_methodMetadataUsage = methodMetadataUsage;
			if (methodReference != null)
			{
				_typeResolver = new TypeResolver(methodReference.DeclaringType as GenericInstanceType, methodReference as GenericInstanceMethod);
			}
			else
			{
				_typeResolver = TypeResolver.Empty;
			}
			_methodUsage = methodUsage;
		}

		public string StaticData(TypeReference type)
		{
			throw new NotSupportedException();
		}

		public string TypeInfoFor(TypeReference type)
		{
			return UnresolvedTypeInfoFor(_typeResolver.Resolve(type));
		}

		public string UnresolvedTypeInfoFor(TypeReference type)
		{
			_context.Global.Collectors.TinyTypes.Add(type);
			_methodMetadataUsage.AddTypeInfo(new UncollectedIl2CppRuntimeType(type));
			return "LookupTypeInfoFromCursor(" + _context.Global.Services.Naming.TinyTypeOffsetNameFor(type) + ")";
		}

		public string Il2CppTypeFor(TypeReference type)
		{
			return TypeInfoFor(type);
		}

		public string ArrayInfo(TypeReference elementType)
		{
			ArrayType type = new ArrayType(_typeResolver.Resolve(elementType));
			return UnresolvedTypeInfoFor(type);
		}

		public string MethodInfo(MethodReference method)
		{
			_methodUsage.AddMethod(method);
			return "&" + _context.Global.Services.Naming.ForRuntimeMethodInfo(method);
		}

		public string HiddenMethodInfo(MethodReference method)
		{
			return "NULL";
		}

		public string Newobj(MethodReference ctor)
		{
			throw new NotSupportedException();
		}

		public string Method(MethodReference method)
		{
			if (MethodVerifier.IsNonGenericMethodThatDoesntExist(method))
			{
				method = method.Resolve();
			}
			MethodReference methodReference = _typeResolver.Resolve(method);
			_methodUsage.AddMethod(methodReference);
			if (!methodReference.ContainsGenericParameter)
			{
				_context.Global.Collectors.TinyTypes.Add(methodReference.ReturnType);
			}
			if (method.ShouldInline(_context.Global.Parameters))
			{
				return _context.Global.Services.Naming.ForMethod(methodReference) + "_inline";
			}
			return _context.Global.Services.Naming.ForMethod(methodReference);
		}

		public string FieldInfo(FieldReference field)
		{
			throw new NotSupportedException();
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
			if (!_stringTypeCached)
			{
				_context.Global.Collectors.TinyTypes.Add(_context.Global.Services.TypeProvider.StringTypeReference);
				_stringTypeCached = true;
			}
			_context.Global.Collectors.TinyStrings.Add(literal);
			_methodMetadataUsage.AddStringLiteral(literal, assemblyDefinition, token);
			return "LookupStringFromCursor(" + _context.Global.Services.Naming.TinyStringOffsetNameFor(literal) + ")";
		}

		public bool NeedsBoxingForValueTypeThis(MethodReference method)
		{
			return false;
		}

		public void StartInitMetadataInline()
		{
		}

		public void EndInitMetadataInline()
		{
		}
	}
}
