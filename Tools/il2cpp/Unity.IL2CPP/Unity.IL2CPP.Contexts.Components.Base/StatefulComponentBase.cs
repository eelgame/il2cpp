using System.Text;
using Unity.IL2CPP.Diagnostics;

namespace Unity.IL2CPP.Contexts.Components.Base
{
	public abstract class StatefulComponentBase<TWrite, TRead, TFull> : ComponentBase<TWrite, TRead, TFull>, IDumpableState where TFull : ComponentBase<TWrite, TRead, TFull>, TWrite, TRead
	{
		void IDumpableState.DumpState(StringBuilder builder)
		{
			DumpState(builder);
		}

		protected abstract void DumpState(StringBuilder builder);
	}
}
