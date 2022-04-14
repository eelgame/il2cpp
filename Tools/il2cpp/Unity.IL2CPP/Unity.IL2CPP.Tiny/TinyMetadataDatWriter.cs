using System;
using System.IO;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Metadata.Dat;

namespace Unity.IL2CPP.Tiny
{
	public class TinyMetadataDatWriter : MetadataDatWriterBase
	{
		protected override int Version
		{
			get
			{
				throw new NotSupportedException();
			}
		}

		public TinyMetadataDatWriter(MetadataWriteContext context)
			: base(context)
		{
		}

		public override void Write()
		{
			using (new FileStream(_context.Global.InputData.MetadataFolder.EnsureDirectoryExists().Combine("tiny_build").ToString(), FileMode.Create, FileAccess.Write))
			{
			}
		}

		protected override void WriteToStream(Stream binary, MemoryStream headerStream, MemoryStream dataStream)
		{
			throw new NotSupportedException();
		}
	}
}
