using Mono.Cecil;

namespace Unity.IL2CPP.StackAnalysis
{
	public class GlobalVariable
	{
		public int BlockIndex { get; internal set; }

		public int Index { get; internal set; }

		public TypeReference Type { get; internal set; }

		public string VariableName => $"G_B{BlockIndex}_{Index}";
	}
}
