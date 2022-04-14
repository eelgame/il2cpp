using System.Collections.Generic;
using Mono.Cecil;
using Unity.Cecil.Awesome.Comparers;

namespace Unity.IL2CPP
{
	public class MethodUsage
	{
		private readonly HashSet<MethodReference> _methods = new HashSet<MethodReference>(new MethodReferenceComparer());

		public void AddMethod(MethodReference method)
		{
			_methods.Add(method);
		}

		public IEnumerable<MethodReference> GetMethods()
		{
			return _methods;
		}
	}
}
