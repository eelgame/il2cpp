using Mono.Cecil;

namespace Unity.IL2CPP
{
	public static class MetadataTokenUtils
	{
		public static string FormattedMetadataTokenFor(FieldReference fieldRef)
		{
			return $"0x{MetadataTokenFor(fieldRef):X8} /* {fieldRef.FullName} */";
		}

		public static uint MetadataTokenFor(TypeReference typeReference)
		{
			return ResolvedTypeFor(typeReference).MetadataToken.ToUInt32();
		}

		public static uint MetadataTokenFor(TypeDefinition typeDefinition)
		{
			return typeDefinition.MetadataToken.ToUInt32();
		}

		public static uint MetadataTokenFor(MethodReference methodReference)
		{
			return ResolvedMethodFor(methodReference).MetadataToken.ToUInt32();
		}

		public static uint MetadataTokenFor(MethodDefinition methodDefinition)
		{
			return methodDefinition.MetadataToken.ToUInt32();
		}

		public static uint MetadataTokenFor(FieldReference fieldReference)
		{
			FieldDefinition fieldDefinition = fieldReference.Resolve();
			if (fieldDefinition == null)
			{
				return fieldReference.MetadataToken.ToUInt32();
			}
			return MetadataTokenFor(fieldDefinition);
		}

		public static uint MetadataTokenFor(FieldDefinition fieldDefinition)
		{
			return fieldDefinition.MetadataToken.ToUInt32();
		}

		public static AssemblyDefinition AssemblyDefinitionFor(TypeReference typeReference)
		{
			return ResolvedTypeFor(typeReference).Module.Assembly;
		}

		public static AssemblyDefinition AssemblyDefinitionFor(TypeDefinition typeDefinition)
		{
			return typeDefinition.Module.Assembly;
		}

		public static AssemblyDefinition AssemblyDefinitionFor(MethodReference methodReference)
		{
			return ResolvedMethodFor(methodReference).Module.Assembly;
		}

		public static AssemblyDefinition AssemblyDefinitionFor(MethodDefinition methodDefinition)
		{
			return methodDefinition.Module.Assembly;
		}

		public static TypeReference ResolvedTypeFor(TypeReference typeReference)
		{
			TypeReference typeReference2 = typeReference;
			if (!typeReference2.IsGenericInstance && !typeReference.IsArray)
			{
				typeReference2 = typeReference.Resolve() ?? typeReference2;
			}
			return typeReference2;
		}

		private static MethodReference ResolvedMethodFor(MethodReference methodReference)
		{
			MethodReference methodReference2 = methodReference;
			if (!methodReference.IsGenericInstance && !methodReference.DeclaringType.IsGenericInstance)
			{
				methodReference2 = methodReference.Resolve() ?? methodReference2;
			}
			return methodReference2;
		}
	}
}
