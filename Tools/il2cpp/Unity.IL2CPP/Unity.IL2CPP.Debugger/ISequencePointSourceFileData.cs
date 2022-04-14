namespace Unity.IL2CPP.Debugger
{
	public interface ISequencePointSourceFileData
	{
		string File { get; }

		byte[] Hash { get; }
	}
}
