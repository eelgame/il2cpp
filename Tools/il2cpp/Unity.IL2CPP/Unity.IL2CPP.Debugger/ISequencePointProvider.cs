using Mono.Cecil;

namespace Unity.IL2CPP.Debugger
{
	public interface ISequencePointProvider
	{
		SequencePointInfo GetSequencePointAt(MethodDefinition method, int ilOffset, SequencePointKind kind);

		bool TryGetSequencePointAt(MethodDefinition method, int ilOffset, SequencePointKind kind, out SequencePointInfo info);

		int GetSeqPointIndex(SequencePointInfo seqPoint);

		bool MethodHasSequencePoints(MethodDefinition method);

		bool MethodHasPausePointAtOffset(MethodDefinition method, int offset);
	}
}
