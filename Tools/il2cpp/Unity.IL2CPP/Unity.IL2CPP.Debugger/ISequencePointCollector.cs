using System.Collections.ObjectModel;
using Mono.Cecil;

namespace Unity.IL2CPP.Debugger
{
	public interface ISequencePointCollector
	{
		int NumSeqPoints { get; }

		SequencePointInfo GetSequencePointAt(MethodDefinition method, int ilOffset, SequencePointKind kind);

		bool TryGetSequencePointAt(MethodDefinition method, int ilOffset, SequencePointKind kind, out SequencePointInfo info);

		ReadOnlyCollection<SequencePointInfo> GetAllSequencePoints();

		int GetSeqPointIndex(SequencePointInfo seqPoint);

		int GetSourceFileIndex(string sourceFile);

		ReadOnlyCollection<ISequencePointSourceFileData> GetAllSourceFiles();

		ReadOnlyCollection<string> GetAllContextInfoStrings();

		ReadOnlyCollection<VariableData> GetVariables();

		bool TryGetVariableRange(MethodDefinition method, out Range range);

		ReadOnlyCollection<Range> GetScopes();

		bool TryGetScopeRange(MethodDefinition method, out Range range);

		bool MethodHasSequencePoints(MethodDefinition method);

		bool MethodHasPausePointAtOffset(MethodDefinition method, int offset);
	}
}
