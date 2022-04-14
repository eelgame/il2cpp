using System.Text;
using Unity.IL2CPP.Diagnostics;

namespace Unity.IL2CPP.Contexts.Components.Base
{
	public abstract class CompletableStatefulComponentBase<TComplete, TWrite, TFull> : CompletableComponentBase<TComplete, TWrite, TFull>, IDumpableState where TFull : CompletableStatefulComponentBase<TComplete, TWrite, TFull>, TWrite
	{
		void IDumpableState.DumpState(StringBuilder builder)
		{
			DumpState(builder);
		}

		protected abstract void DumpState(StringBuilder builder);
	}
}
