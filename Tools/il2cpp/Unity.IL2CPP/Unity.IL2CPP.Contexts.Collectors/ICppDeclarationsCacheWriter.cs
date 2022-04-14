using System.Collections.Generic;
using Mono.Cecil;

namespace Unity.IL2CPP.Contexts.Collectors
{
	public interface ICppDeclarationsCacheWriter : ICppIncludeDepthCalculatorCache
	{
		void PopulateCache(SourceWritingContext context, IEnumerable<TypeReference> rootTypes);
	}
}
