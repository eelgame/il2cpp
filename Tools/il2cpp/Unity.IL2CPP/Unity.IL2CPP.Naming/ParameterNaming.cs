using System.Globalization;
using Mono.Cecil;
using Unity.IL2CPP.Contexts.Services;

namespace Unity.IL2CPP.Naming
{
	public static class ParameterNaming
	{
		public static string ForParameterName(this INamingService naming, ParameterReference parameterReference)
		{
			string name = (string.IsNullOrEmpty(parameterReference.Name) ? "p" : NamingComponent.EscapeKeywords(parameterReference.Name));
			return naming.Clean(name) + parameterReference.Index.ToString(CultureInfo.InvariantCulture);
		}

		public static string ForParameterName(this INamingService naming, TypeReference type, int index)
		{
			string name = (string.IsNullOrEmpty(type.Name) ? "p" : NamingComponent.EscapeKeywords(type.Name));
			return naming.Clean(name) + index.ToString(CultureInfo.InvariantCulture);
		}
	}
}
