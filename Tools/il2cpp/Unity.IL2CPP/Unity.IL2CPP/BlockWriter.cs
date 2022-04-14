using System;
using Unity.IL2CPP.CodeWriters;

namespace Unity.IL2CPP
{
	internal class BlockWriter : IDisposable
	{
		private readonly ICodeWriter _writer;

		private readonly bool _semicolon;

		public BlockWriter(ICodeWriter writer, bool semicolon = false)
		{
			_writer = writer;
			_semicolon = semicolon;
			writer.BeginBlock();
		}

		public void Dispose()
		{
			_writer.EndBlock(_semicolon);
		}
	}
}
