namespace Unity.IL2CPP.Debugger
{
	public struct Range
	{
		public readonly int Start;

		public readonly int Length;

		public Range(int start, int length)
		{
			Start = start;
			Length = length;
		}
	}
}
