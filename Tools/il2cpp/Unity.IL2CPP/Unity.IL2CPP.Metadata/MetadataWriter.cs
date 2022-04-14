using System.Collections.Generic;
using Unity.IL2CPP.CodeWriters;

namespace Unity.IL2CPP.Metadata
{
	public class MetadataWriter<TWriter> where TWriter : ICppCodeWriter
	{
		public enum ArrayTerminator
		{
			None,
			Null
		}

		private readonly TWriter _writer;

		protected TWriter Writer => _writer;

		protected MetadataWriter(TWriter writer)
		{
			_writer = writer;
		}

		protected void WriteLine(string line)
		{
			TWriter writer = _writer;
			writer.WriteLine(line);
		}

		protected void WriteLine(string format, params object[] args)
		{
			TWriter writer = _writer;
			writer.WriteLine(format, args);
		}

		protected void Write(string format)
		{
			TWriter writer = _writer;
			writer.Write(format);
		}

		protected void Write(string format, params object[] args)
		{
			TWriter writer = _writer;
			writer.Write(format, args);
		}

		protected void BeginBlock()
		{
			TWriter writer = _writer;
			writer.BeginBlock();
		}

		protected void EndBlock(bool semicolon)
		{
			TWriter writer = _writer;
			writer.EndBlock(semicolon);
		}

		protected void WriteArrayInitializer(IEnumerable<string> initializers, ArrayTerminator terminator = ArrayTerminator.None)
		{
			BeginBlock();
			foreach (string initializer in initializers)
			{
				WriteLine("{0},", initializer);
			}
			if (terminator == ArrayTerminator.Null)
			{
				WriteLine("NULL");
			}
			EndBlock(semicolon: true);
		}
	}
}
