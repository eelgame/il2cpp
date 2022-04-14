using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Unity.IL2CPP.Debugger
{
	public class SequencePointInfo
	{
		public readonly MethodDefinition Method;

		public readonly SequencePointKind Kind;

		public readonly string SourceFile;

		public readonly byte[] SourceFileHash;

		public readonly int StartLine;

		public readonly int EndLine;

		public readonly int StartColumn;

		public readonly int EndColumn;

		public readonly int IlOffset;

		public const int MethodEntryIlOffset = -1;

		public const int MethodExitIlOffset = 16777215;

		public SequencePointInfo(MethodDefinition method, SequencePoint sequencePoint)
		{
			Method = method;
			Kind = SequencePointKind.Normal;
			SourceFile = sequencePoint.Document.Url;
			SourceFileHash = sequencePoint.Document.Hash;
			StartLine = sequencePoint.StartLine;
			if (sequencePoint.EndLine < 0)
			{
				EndLine = StartLine;
			}
			else
			{
				EndLine = sequencePoint.EndLine;
			}
			StartColumn = sequencePoint.StartColumn;
			EndColumn = sequencePoint.EndColumn;
			IlOffset = sequencePoint.Offset;
		}

		public SequencePointInfo(MethodDefinition method, SequencePointKind kind, string sourceFile, byte[] sourceFileHash, int startLine, int endLine, int startColumn, int endColumn, int ilOffset)
		{
			Method = method;
			Kind = kind;
			SourceFile = sourceFile;
			SourceFileHash = sourceFileHash;
			StartLine = startLine;
			if (endLine < 0)
			{
				EndLine = StartLine;
			}
			else
			{
				EndLine = endLine;
			}
			StartColumn = startColumn;
			EndColumn = endColumn;
			IlOffset = ilOffset;
		}

		public static SequencePointInfo Empty(MethodDefinition method, SequencePointKind kind, int ilOffset)
		{
			return new SequencePointInfo(method, kind, string.Empty, null, -1, -1, -1, -1, ilOffset);
		}
	}
}
