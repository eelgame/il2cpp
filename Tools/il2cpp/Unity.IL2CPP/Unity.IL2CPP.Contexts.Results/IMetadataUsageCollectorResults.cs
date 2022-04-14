using System.Collections.Generic;
using Mono.Cecil;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Metadata.RuntimeTypes;

namespace Unity.IL2CPP.Contexts.Results
{
	public interface IMetadataUsageCollectorResults
	{
		int UsageCount { get; }

		ReadOnlyHashSet<IIl2CppRuntimeType> GetTypeInfos();

		ReadOnlyHashSet<IIl2CppRuntimeType> GetIl2CppTypes();

		ReadOnlyHashSet<MethodReference> GetInflatedMethods();

		ReadOnlyHashSet<Il2CppRuntimeFieldReference> GetFieldInfos();

		ReadOnlyHashSet<StringMetadataToken> GetStringLiterals();

		IReadOnlyCollection<KeyValuePair<string, MethodMetadataUsage>> GetUsages();
	}
}
