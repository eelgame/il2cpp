using Mono.Cecil;

namespace Unity.IL2CPP.GenericSharing
{
	public class RuntimeGenericMethodData : RuntimeGenericData
	{
		public readonly MethodReference GenericMethod;

		public RuntimeGenericMethodData(RuntimeGenericContextInfo infoType, MethodReference genericMethod)
			: base(infoType)
		{
			GenericMethod = genericMethod;
		}
	}
}
