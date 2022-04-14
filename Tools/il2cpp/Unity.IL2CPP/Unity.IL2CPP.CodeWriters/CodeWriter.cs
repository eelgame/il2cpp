using System;
using System.IO;
using System.Text;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Scheduling.Streams;

namespace Unity.IL2CPP.CodeWriters
{
	public class CodeWriter : ICodeWriter, IDisposable, IStream
	{
		private readonly ReadOnlyContext _context;

		private readonly bool _owns;

		private int _indent;

		private bool _shouldIndent;

		private string _indentString = "";

		private readonly string[] _indentStrings;

		private readonly StringBuilder _cleanStringBuilder;

		public StreamWriter Writer { get; private set; }

		public ReadOnlyContext Context => _context;

		public long Length => Writer.BaseStream.Length;

		public int IndentationLevel => _indent;

		public CodeWriter(ReadOnlyContext context, StreamWriter writer, bool owns = true)
		{
			_context = context;
			_owns = owns;
			Writer = writer;
			_shouldIndent = true;
			_indentStrings = new string[10];
			_cleanStringBuilder = new StringBuilder();
			for (int i = 0; i < _indentStrings.Length; i++)
			{
				_indentStrings[i] = new string('\t', i);
			}
		}

		public void WriteLine()
		{
			Writer.Write("\n");
			_shouldIndent = true;
		}

		public void WriteLine(string block)
		{
			WriteIndented(block);
			Writer.Write("\n");
			_shouldIndent = true;
		}

		public void WriteCommentedLine(string block)
		{
			if (_context.Global.Parameters.EmitComments)
			{
				block = block.TrimEnd('\\', '/');
				WriteLine("// {0}", ConvertStringToPrintableAscii(block));
			}
		}

		public void WriteCommentedLine(string format, params object[] parameters)
		{
			WriteCommentedLine(string.Format(format, parameters));
		}

		public void WriteStatement(string block)
		{
			WriteLine($"{block};");
		}

		public void WriteLine(string block, params object[] args)
		{
			if (args.Length != 0)
			{
				block = string.Format(block, args);
			}
			WriteLine(block);
		}

		public void Write(string block)
		{
			WriteIndented(block);
		}

		public void Write(string block, params object[] args)
		{
			if (args.Length != 0)
			{
				block = string.Format(block, args);
			}
			Write(block);
		}

		public void WriteUnindented(string block, params object[] args)
		{
			if (args.Length != 0)
			{
				block = string.Format(block, args);
			}
			Writer.Write(block + "\n");
		}

		public void Indent(int count = 1)
		{
			_indent += count;
			if (_indent < _indentStrings.Length)
			{
				_indentString = _indentStrings[_indent];
			}
			else
			{
				_indentString = new string('\t', _indent);
			}
		}

		public void Dedent(int count = 1)
		{
			if (count > _indent)
			{
				throw new ArgumentException("Cannot dedent CppCodeWriter more than it was indented.", "count");
			}
			_indent -= count;
			if (_indent < _indentStrings.Length)
			{
				_indentString = _indentStrings[_indent];
			}
			else
			{
				_indentString = new string('\t', _indent);
			}
		}

		public void BeginBlock()
		{
			WriteLine("{");
			Indent();
		}

		public void BeginBlock(string comment)
		{
			Write("{ // ");
			WriteLine(comment);
			Indent();
		}

		public void EndBlock(bool semicolon = false)
		{
			Dedent();
			if (semicolon)
			{
				WriteLine("};");
			}
			else
			{
				WriteLine("}");
			}
		}

		public void EndBlock(string comment, bool semicolon = false)
		{
			Dedent();
			Write("}");
			if (semicolon)
			{
				Write(";");
			}
			Write(" // ");
			WriteLine(comment);
		}

		private void WriteIndented(string s)
		{
			if (_shouldIndent)
			{
				Writer.Write(_indentString);
				_shouldIndent = false;
			}
			Writer.Write(s);
		}

		public virtual void Dispose()
		{
			if (_owns)
			{
				Writer.Dispose();
			}
		}

		private string ConvertStringToPrintableAscii(string input)
		{
			byte[] bytes = Encoding.ASCII.GetBytes(input);
			int num = bytes.Length;
			for (int i = 0; i < num; i++)
			{
				if (bytes[i] < 32 || bytes[i] >= 127)
				{
					bytes[i] = 63;
				}
			}
			return Encoding.ASCII.GetString(bytes);
		}
	}
}
