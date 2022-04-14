using Mono.Cecil;

namespace Unity.IL2CPP.GenericsCollection
{
	public class GenericContext
	{
		private readonly GenericInstanceType _type;

		private readonly GenericInstanceMethod _method;

		public GenericInstanceType Type => _type;

		public GenericInstanceMethod Method => _method;

		public GenericContext(GenericInstanceType type, GenericInstanceMethod method)
		{
			_type = type;
			_method = method;
		}
	}
}
