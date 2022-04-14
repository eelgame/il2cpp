using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using NiceIO;

namespace Unity.IL2CPP.AssemblyConversion.PerAssembly.Master
{
	internal class SlaveDispatcher
	{
		[DebuggerDisplay("{Data}")]
		private class WorkItem
		{
			public SlaveInstanceData Data;

			public Shell.ExecuteArgs Args;

			public Shell.ExecuteAsyncResult Process;
		}

		private readonly SlaveInstanceData[] _slaveData;

		private readonly NPath _il2CppExecutablePath;

		private int MaxConcurrent => 1;

		public SlaveDispatcher(SlaveInstanceData[] slaveData)
		{
			_slaveData = slaveData;
			_il2CppExecutablePath = DetermineBestIl2CppExecutablePath();
		}

		public static void Start()
		{
			throw new NotImplementedException("TODO by Mike : Not sure if async will be needed");
		}

		public SlaveInstanceResult[] WaitForCompletion()
		{
			throw new NotImplementedException("TODO by Mike : Not sure if async will be needed");
		}

		public SlaveInstanceResult[] Run()
		{
			Queue<WorkItem> queue = new Queue<WorkItem>();
			SlaveInstanceData[] slaveData = _slaveData;
			foreach (SlaveInstanceData data in slaveData)
			{
				queue.Enqueue(CreateExecuteArgs(data));
			}
			List<WorkItem> list = new List<WorkItem>();
			List<SlaveInstanceResult> list2 = new List<SlaveInstanceResult>();
			bool oneOrMoreFailed = false;
			while (queue.Count > 0)
			{
				FillActive(queue, list);
				WaitHandle.WaitAny(list.Select((WorkItem a) => a.Process.WaitHandle).ToArray());
				ProcessExited(list, list2, out oneOrMoreFailed);
				if (oneOrMoreFailed)
				{
					WaitingForActiveToComplete(list, list2);
					SkipQueued(queue, list2);
				}
			}
			return list2.ToArray();
		}

		private void WaitingForActiveToComplete(List<WorkItem> active, List<SlaveInstanceResult> completed)
		{
			foreach (WorkItem item2 in active)
			{
				SlaveInstanceResult item = CreateResult(item2, item2.Process.Wait());
				completed.Add(item);
			}
			active.Clear();
		}

		private void SkipQueued(Queue<WorkItem> workQueue, List<SlaveInstanceResult> completed)
		{
			while (workQueue.Count > 0)
			{
				WorkItem workItem = workQueue.Dequeue();
				completed.Add(CreateResult(workItem, null));
			}
		}

		private void ProcessExited(List<WorkItem> active, List<SlaveInstanceResult> completed, out bool oneOrMoreFailed)
		{
			oneOrMoreFailed = false;
			for (int i = 0; i < active.Count; i++)
			{
				WorkItem workItem = active[i];
				if (workItem.Process.HasExited)
				{
					SlaveInstanceResult slaveInstanceResult = CreateResult(workItem, workItem.Process.Wait());
					completed.Add(slaveInstanceResult);
					active.RemoveAt(i--);
					if (!oneOrMoreFailed)
					{
						oneOrMoreFailed = slaveInstanceResult.Failed;
					}
				}
			}
		}

		private void FillActive(Queue<WorkItem> workQueue, List<WorkItem> active)
		{
			while (active.Count < MaxConcurrent && workQueue.Count > 0)
			{
				WorkItem workItem = workQueue.Dequeue();
				workItem.Process = Shell.ExecuteAsync(workItem.Args);
				active.Add(workItem);
			}
		}

		private WorkItem CreateExecuteArgs(SlaveInstanceData data)
		{
			return new WorkItem
			{
				Data = data,
				Args = new Shell.ExecuteArgs
				{
					Executable = _il2CppExecutablePath.ToString(),
					Arguments = data.Arguments.AggregateWithSpace()
				}
			};
		}

		private static SlaveInstanceResult CreateResult(WorkItem workItem, Shell.ExecuteResult shellResult)
		{
			return new SlaveInstanceResult(workItem.Args, workItem.Data, shellResult);
		}

		private static NPath DetermineBestIl2CppExecutablePath()
		{
			NPath nPath = GetCurrentAssemblyDirectory().Combine("il2cpp.exe");
			if (nPath.Exists())
			{
				return nPath;
			}
			throw new NotImplementedException("Could not locate il2cpp");
		}

		private static NPath GetCurrentAssemblyDirectory()
		{
			Uri uri = new Uri(typeof(SlaveDispatcher).Assembly.CodeBase);
			string text = uri.LocalPath;
			if (!string.IsNullOrEmpty(uri.Fragment))
			{
				text += Uri.UnescapeDataString(uri.Fragment);
			}
			return text.ToNPath().Parent;
		}
	}
}
