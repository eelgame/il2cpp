using System;
using System.IO;
using System.Text;
using NiceIO;
using Unity.IL2CPP.Contexts;

namespace Unity.IL2CPP.CodeWriters
{
	public class GeneratedCodeSourceCodeWriter : InMemoryCodeWriter
	{
		private readonly IDisposable _profilerSection;

		private readonly NPath _filename;

		public GeneratedCodeSourceCodeWriter(SourceWritingContext context, NPath filename, IDisposable profilerSection)
			: base(context)
		{
			if (filename.HasExtension("h", "hh", "hpp"))
			{
				throw new InvalidOperationException("SourceCodeWriter can only be used to write source files");
			}
			context.Global.Collectors.Stats.RecordFileWritten(filename);
			_profilerSection = profilerSection;
			_filename = filename;
		}

		public override void Dispose()
		{
			try
			{
				using (StreamWriter streamWriter = new StreamWriter(File.Open(_filename.ToString(), FileMode.Create), Encoding.UTF8))
				{
					SourceCodeWriterUtils.WriteCommonIncludes(streamWriter, _filename);
					CppDeclarationsWriter.Write(base.Context, streamWriter, base.Declarations);
					base.Writer.Flush();
					base.Writer.BaseStream.Seek(0L, SeekOrigin.Begin);
					base.Writer.BaseStream.CopyTo(streamWriter.BaseStream);
					streamWriter.Flush();
				}
				base.Dispose();
			}
			finally
			{
				_profilerSection?.Dispose();
			}
		}
	}
}
