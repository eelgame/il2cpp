using System.Collections.Generic;
using System.Collections.ObjectModel;
using Mono.Cecil;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.GenericSharing;

namespace Unity.IL2CPP
{
	public class ReadOnlyInvokerCollection
	{
		private readonly ReadOnlyDictionary<InvokerSignature, int> _runtimeInvokerData;

		public ReadOnlyInvokerCollection(Dictionary<InvokerSignature, int> runtimeInvokerData)
		{
			_runtimeInvokerData = runtimeInvokerData.AsReadOnly();
		}

		public int GetIndex(ReadOnlyContext context, MethodReference method)
		{
			if (method.HasGenericParameters || method.DeclaringType.HasGenericParameters)
			{
				return -1;
			}
			if (GenericSharingAnalysis.CanShareMethod(context, method))
			{
				method = GenericSharingAnalysis.GetSharedMethod(context, method);
			}
			InvokerSignature key = new InvokerSignature(method.HasThis, TypeCollapser.CollapseSignature(context, method));
			if (!_runtimeInvokerData.TryGetValue(key, out var value))
			{
				return -1;
			}
			return value;
		}

		public ReadOnlyCollection<InvokerSignature> GetInvokers()
		{
			return _runtimeInvokerData.KeysSortedByValue();
		}
	}
}
