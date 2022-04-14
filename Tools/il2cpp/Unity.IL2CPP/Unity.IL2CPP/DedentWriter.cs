using System;
using Unity.IL2CPP.CodeWriters;

namespace Unity.IL2CPP
{
	internal class DedentWriter : IDisposable
	{
		private readonly ICodeWriter _writer;

		public DedentWriter(ICodeWriter writer)
		{
			_writer = writer;
			writer.Dedent();
		}

		public void Dispose()
		{
			_writer.Indent();
		}
	}
}
