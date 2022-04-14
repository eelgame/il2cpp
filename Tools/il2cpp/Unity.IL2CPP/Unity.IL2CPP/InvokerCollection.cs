using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Unity.Cecil.Awesome.Ordering;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.GenericSharing;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP
{
	public class InvokerCollection : IInvokerCollector
	{
		private class InvokerSignatureEqualityComparer : EqualityComparer<InvokerSignature>
		{
			private readonly TypeReferenceArrayEqualityComparer typeReferenceArrayComparer = new TypeReferenceArrayEqualityComparer();

			public override bool Equals(InvokerSignature x, InvokerSignature y)
			{
				if (x.HasThis == y.HasThis)
				{
					return typeReferenceArrayComparer.Equals(x.ReducedParameterTypes, y.ReducedParameterTypes);
				}
				return false;
			}

			public override int GetHashCode(InvokerSignature obj)
			{
				bool hasThis = obj.HasThis;
				return HashCodeHelper.Combine(hasThis.GetHashCode(), typeReferenceArrayComparer.GetHashCode(obj.ReducedParameterTypes));
			}
		}

		private class InvokerSignatureComparer : Comparer<InvokerSignature>
		{
			public override int Compare(InvokerSignature x, InvokerSignature y)
			{
				if (x.HasThis)
				{
					if (!y.HasThis)
					{
						return -1;
					}
				}
				else if (y.HasThis)
				{
					return 1;
				}
				return x.ReducedParameterTypes.Compare(y.ReducedParameterTypes);
			}
		}

		private readonly HashSet<InvokerSignature> _runtimeInvokerData = new HashSet<InvokerSignature>(new InvokerSignatureEqualityComparer());

		private bool _complete;

		public void Add(InvokerCollection other)
		{
			_runtimeInvokerData.UnionWith(other._runtimeInvokerData);
		}

		public string Add(ReadOnlyContext context, MethodReference method)
		{
			if (_complete)
			{
				throw new InvalidOperationException("This collection has already been completed");
			}
			if (GenericSharingAnalysis.CanShareMethod(context, method))
			{
				method = GenericSharingAnalysis.GetSharedMethod(context, method);
			}
			InvokerSignature invokerSignature = new InvokerSignature(method.HasThis, TypeCollapser.CollapseSignature(context, method));
			_runtimeInvokerData.Add(invokerSignature);
			return NameForInvoker(context, invokerSignature);
		}

		public ReadOnlyInvokerCollection Complete()
		{
			_complete = true;
			List<InvokerSignature> list = _runtimeInvokerData.ToList();
			list.Sort(new InvokerSignatureComparer());
			Dictionary<InvokerSignature, int> dictionary = new Dictionary<InvokerSignature, int>(new InvokerSignatureEqualityComparer());
			foreach (InvokerSignature item in list)
			{
				dictionary.Add(item, dictionary.Count);
			}
			return new ReadOnlyInvokerCollection(dictionary);
		}

		internal static string NameForInvoker(ReadOnlyContext context, InvokerSignature data)
		{
			INamingService naming = context.Global.Services.Naming;
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(naming.ForMetadataGlobalVar("RuntimeInvoker_"));
			stringBuilder.Append(data.HasThis);
			stringBuilder.Append(naming.ForType(data.ReducedParameterTypes[0]));
			for (int i = 1; i < data.ReducedParameterTypes.Length; i++)
			{
				stringBuilder.Append("_");
				stringBuilder.Append(naming.ForType(data.ReducedParameterTypes[i]));
			}
			return stringBuilder.ToString();
		}
	}
}
