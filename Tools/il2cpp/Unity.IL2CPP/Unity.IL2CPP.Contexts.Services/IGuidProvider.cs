using System;
using Mono.Cecil;

namespace Unity.IL2CPP.Contexts.Services
{
	public interface IGuidProvider
	{
		Guid GuidFor(ReadOnlyContext context, TypeReference type);
	}
}
