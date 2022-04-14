using System;
using System.IO;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Scheduling.Streams;

namespace Unity.IL2CPP.CodeWriters
{
	public interface ICodeWriter : IDisposable, IStream
	{
		ReadOnlyContext Context { get; }

		StreamWriter Writer { get; }

		int IndentationLevel { get; }

		void WriteLine();

		void WriteLine(string block);

		void WriteLine(string format, params object[] args);

		void WriteCommentedLine(string block);

		void WriteCommentedLine(string format, params object[] args);

		void WriteStatement(string block);

		void Write(string block);

		void Write(string format, params object[] args);

		void WriteUnindented(string block, params object[] args);

		void Indent(int count = 1);

		void Dedent(int count = 1);

		void BeginBlock();

		void BeginBlock(string comment);

		void EndBlock(bool semicolon = false);

		void EndBlock(string comment, bool semicolon = false);
	}
}
