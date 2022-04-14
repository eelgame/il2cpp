using System.Collections.Generic;
using Mono.Cecil;
using Unity.IL2CPP.Contexts;

namespace Unity.IL2CPP.Debugger
{
	public class CatchPointCollector : ICatchPointCollector, ICatchPointProvider
	{
		private readonly Dictionary<MethodDefinition, HashSet<CatchPointInfo>> catchPointsByMethod = new Dictionary<MethodDefinition, HashSet<CatchPointInfo>>();

		private readonly List<CatchPointInfo> allCatchPoints = new List<CatchPointInfo>();

		private static readonly CatchPointInfoComparer s_catchPointComparer = new CatchPointInfoComparer();

		public int NumCatchPoints => allCatchPoints.Count;

		public IEnumerable<CatchPointInfo> AllCatchPoints => allCatchPoints.AsReadOnly();

		public void AddCatchPoint(PrimaryCollectionContext context, MethodDefinition method, ExceptionSupport.Node catchNode)
		{
			if (!catchPointsByMethod.TryGetValue(method, out var value))
			{
				value = (catchPointsByMethod[method] = new HashSet<CatchPointInfo>(s_catchPointComparer));
			}
			CatchPointInfo item = new CatchPointInfo((catchNode?.Handler?.CatchType != null) ? context.Global.Collectors.Types.Add(catchNode.Handler.CatchType) : null, method, catchNode);
			value.Add(item);
			allCatchPoints.Add(item);
		}
	}
}
