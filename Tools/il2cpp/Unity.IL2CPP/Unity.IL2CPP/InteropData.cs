namespace Unity.IL2CPP
{
	public sealed class InteropData
	{
		public bool HasDelegatePInvokeWrapperMethod { get; set; }

		public bool HasPInvokeMarshalingFunctions { get; set; }

		public bool HasCreateCCWFunction { get; set; }

		public bool HasGuid { get; set; }
	}
}
