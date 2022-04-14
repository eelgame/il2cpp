using System;

namespace Unity.IL2CPP.AssemblyConversion.Steps.Results
{
	public class GlobalPendingResults<TResult>
	{
		private TResult _result;

		private bool _complete;

		public void SetResults(TResult result)
		{
			_result = result;
			_complete = true;
		}

		public TResult GetResults()
		{
			if (!_complete)
			{
				throw new InvalidOperationException("Cannot get results until collection of the results has completed");
			}
			return _result;
		}
	}
}
