using System.Collections.Generic;
using System.Collections.ObjectModel;
using Unity.MiniProfiling;

namespace Unity.IL2CPP.Statistics
{
	public class ProfilerSnapshot
	{
		private readonly ReadOnlyCollection<MiniProfiler.ThreadContext> _profilerData;

		public ProfilerSnapshot(ReadOnlyCollection<MiniProfiler.ThreadContext> profilerData)
		{
			_profilerData = profilerData;
		}

		public static ProfilerSnapshot Capture()
		{
			return new ProfilerSnapshot(MiniProfiler.CaptureSnapshot());
		}

		public IEnumerable<MiniProfiler.TimedSection> GetSectionsByLabel(string label)
		{
			foreach (MiniProfiler.ThreadContext profilerDatum in _profilerData)
			{
				foreach (MiniProfiler.TimedSection section in profilerDatum.Sections)
				{
					if (section.Label == label)
					{
						yield return section;
					}
				}
			}
		}
	}
}
