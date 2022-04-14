using System;
using Mono.Cecil;

namespace Unity.IL2CPP
{
	public class ArrayUtilities
	{
		public static TypeReference ArrayElementTypeOf(TypeReference typeReference)
		{
			if (typeReference is ArrayType arrayType)
			{
				return arrayType.ElementType;
			}
			if (typeReference is TypeSpecification typeSpecification)
			{
				return ArrayElementTypeOf(typeSpecification.ElementType);
			}
			throw new ArgumentException($"{typeReference.FullName} is not an array type", "typeReference");
		}
	}
}
