namespace Unity.IL2CPP.Marshaling
{
	public class MarshaledType
	{
		public readonly string Name;

		public readonly string DecoratedName;

		public readonly string VariableName;

		public MarshaledType(string name, string decoratedName)
		{
			Name = name;
			DecoratedName = decoratedName;
			VariableName = string.Empty;
		}

		public MarshaledType(string name, string decoratedName, string variableName)
		{
			Name = name;
			DecoratedName = decoratedName;
			VariableName = variableName;
		}
	}
}
