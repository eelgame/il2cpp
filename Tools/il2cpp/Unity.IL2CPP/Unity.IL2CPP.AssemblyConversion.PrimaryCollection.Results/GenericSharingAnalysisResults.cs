using System.Collections.Generic;
using System.Collections.ObjectModel;
using Mono.Cecil;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.GenericSharing;

namespace Unity.IL2CPP.AssemblyConversion.PrimaryCollection.Results
{
	public sealed class GenericSharingAnalysisResults
	{
		private readonly ReadOnlyDictionary<TypeDefinition, GenericSharingData> _genericTypeData;

		private readonly ReadOnlyDictionary<MethodDefinition, GenericSharingData> _genericMethodData;

		public static GenericSharingAnalysisResults Empty => new GenericSharingAnalysisResults(new Dictionary<TypeDefinition, GenericSharingData>().AsReadOnly(), new Dictionary<MethodDefinition, GenericSharingData>().AsReadOnly());

		public GenericSharingAnalysisResults(ReadOnlyDictionary<TypeDefinition, GenericSharingData> genericTypeData, ReadOnlyDictionary<MethodDefinition, GenericSharingData> genericMethodData)
		{
			_genericTypeData = genericTypeData;
			_genericMethodData = genericMethodData;
		}

		public GenericSharingData RuntimeGenericContextFor(TypeDefinition type)
		{
			if (!_genericTypeData.TryGetValue(type, out var value))
			{
				return GenericSharingData.Empty;
			}
			return value;
		}

		public GenericSharingData RuntimeGenericContextFor(MethodDefinition method)
		{
			if (!_genericMethodData.TryGetValue(method, out var value))
			{
				return GenericSharingData.Empty;
			}
			return value;
		}
	}
}
