using System.Collections.Generic;
using Mono.Cecil;

namespace Unity.IL2CPP.Contexts.Collectors
{
	public interface IVirtualCallCollector
	{
		void Add(SourceWritingContext context, MethodReference method);

		void AddRange(SourceWritingContext context, IEnumerable<MethodReference> methods);
	}
}
