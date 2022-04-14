using System.Text;

namespace Unity.IL2CPP.Diagnostics
{
	public interface IDumpableState
	{
		void DumpState(StringBuilder builder);
	}
}
