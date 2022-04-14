using System.IO;
using System.Text;
using Unity.IL2CPP.Contexts;

namespace Unity.IL2CPP.CodeWriters
{
	public class InMemoryCodeWriter : GeneratedCodeWriter
	{
		private readonly MemoryStream _memoryStream;

		public const int DefaultMemoryStreamCapacity = 4096;

		public InMemoryCodeWriter(SourceWritingContext context)
			: this(context, new MemoryStream(4096))
		{
		}

		private InMemoryCodeWriter(SourceWritingContext context, MemoryStream stream)
			: base(context, new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)))
		{
			_memoryStream = stream;
		}

		public string GetSourceCodeString()
		{
			base.Writer.Flush();
			_memoryStream.Flush();
			return Encoding.UTF8.GetString(_memoryStream.ToArray());
		}
	}
}
