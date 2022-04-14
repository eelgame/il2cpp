using System;
using System.Collections.Generic;
using System.Threading;

namespace Unity.IL2CPP.Building
{
	internal static class ParallelFor
	{
		public static void Run<D>(D[] data, Action<D> action)
		{
			int nextJobToTake = 0;
			List<Thread> list = new List<Thread>();
			List<Exception> exceptions = new List<Exception>();
			for (int i = 0; i < Environment.ProcessorCount; i++)
			{
				list.Add(new Thread((ParameterizedThreadStart)delegate
				{
					try
					{
						while (true)
						{
							int num = Interlocked.Increment(ref nextJobToTake) - 1;
							if (num >= data.Length)
							{
								break;
							}
							action(data[num]);
						}
					}
					catch (Exception item)
					{
						lock (exceptions)
						{
							exceptions.Add(item);
						}
					}
				}));
			}
			foreach (Thread item2 in list)
			{
				item2.Start();
			}
			foreach (Thread item3 in list)
			{
				item3.Join();
			}
			lock (exceptions)
			{
				if (exceptions.Count > 0)
				{
					throw new AggregateException(exceptions);
				}
			}
		}

		public static IEnumerable<T> RunWithResult<D, T>(D[] data, Func<D, T> action, int threadSuggestion = 0)
		{
			int nextJobToTake = 0;
			T[] results = new T[data.Length];
			List<Exception> exceptions = new List<Exception>();
			List<Thread> list = new List<Thread>();
			if (threadSuggestion == 0)
			{
				threadSuggestion = Environment.ProcessorCount;
			}
			threadSuggestion = Math.Min(Environment.ProcessorCount, threadSuggestion);
			for (int i = 0; i < threadSuggestion; i++)
			{
				list.Add(new Thread((ParameterizedThreadStart)delegate
				{
					try
					{
						while (true)
						{
							int num = Interlocked.Increment(ref nextJobToTake) - 1;
							if (num >= data.Length)
							{
								break;
							}
							D arg = data[num];
							T val = action(arg);
							lock (results)
							{
								results[num] = val;
							}
						}
					}
					catch (Exception item)
					{
						lock (exceptions)
						{
							exceptions.Add(item);
						}
					}
				}));
			}
			foreach (Thread item2 in list)
			{
				item2.Start();
			}
			foreach (Thread item3 in list)
			{
				item3.Join();
			}
			lock (exceptions)
			{
				if (exceptions.Count > 0)
				{
					throw new AggregateException(exceptions);
				}
			}
			return results;
		}
	}
}
