using System.Collections.ObjectModel;

namespace Unity.IL2CPP.AssemblyConversion.Steps.Results
{
	public class ReadOnlyPendingResults<TWorkerItem, TWorkerResult>
	{
		private readonly PendingResults<TWorkerItem, TWorkerResult> _pending;

		public ReadOnlyDictionary<TWorkerItem, TWorkerResult> Result => _pending.GetResults();

		public ReadOnlyPendingResults(PendingResults<TWorkerItem, TWorkerResult> pending)
		{
			_pending = pending;
		}
	}
}
