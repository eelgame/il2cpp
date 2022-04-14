namespace Unity.IL2CPP.AssemblyConversion.Steps.Results
{
	public sealed class ReadOnlyGlobalPendingResults<TResult>
	{
		private readonly GlobalPendingResults<TResult> _pendingResults;

		public TResult Result => _pendingResults.GetResults();

		public ReadOnlyGlobalPendingResults(GlobalPendingResults<TResult> pendingResults)
		{
			_pendingResults = pendingResults;
		}
	}
}
