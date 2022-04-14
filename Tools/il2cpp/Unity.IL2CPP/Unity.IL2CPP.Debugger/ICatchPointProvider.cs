using System.Collections.Generic;

namespace Unity.IL2CPP.Debugger
{
	public interface ICatchPointProvider
	{
		int NumCatchPoints { get; }

		IEnumerable<CatchPointInfo> AllCatchPoints { get; }
	}
}
