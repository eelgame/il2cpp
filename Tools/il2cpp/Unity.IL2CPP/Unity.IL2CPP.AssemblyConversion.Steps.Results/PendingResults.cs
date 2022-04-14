using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts.Services;

namespace Unity.IL2CPP.AssemblyConversion.Steps.Results
{
	public class PendingResults<TWorkerItem, TWorkerResult>
	{
		private ReadOnlyCollection<ResultData<TWorkerItem, TWorkerResult>> _results;

		public void SetResultsAsEmpty()
		{
			_results = new List<ResultData<TWorkerItem, TWorkerResult>>().AsReadOnly();
		}

		public void SetResults(ReadOnlyCollection<ResultData<TWorkerItem, TWorkerResult>> collectedData)
		{
			_results = collectedData;
		}

		public ReadOnlyDictionary<TWorkerItem, TWorkerResult> GetResults()
		{
			if (_results == null)
			{
				throw new InvalidOperationException("Cannot get results until collection of the results has completed");
			}
			Dictionary<TWorkerItem, TWorkerResult> dictionary = new Dictionary<TWorkerItem, TWorkerResult>();
			foreach (ResultData<TWorkerItem, TWorkerResult> result in _results)
			{
				dictionary.Add(result.Item, result.Result);
			}
			return dictionary.AsReadOnly();
		}
	}
}
