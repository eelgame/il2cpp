using Unity.IL2CPP.AssemblyConversion;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.StringLiterals;
using Unity.MiniProfiling;

namespace Unity.IL2CPP.Metadata
{
	internal class BigMetadataWriter : IMetadataWriterImplementation
	{
		private readonly SourceWritingContext _context;

		public BigMetadataWriter(SourceWritingContext context)
		{
			_context = context;
		}

		public void Write(out IStringLiteralCollection stringLiteralCollection, out IFieldReferenceCollection fieldReferenceCollection)
		{
			using (MiniProfiler.Section("WriteMetadata"))
			{
				new MetadataCacheWriter(_context).Write(out stringLiteralCollection, out fieldReferenceCollection);
				_context.Global.Services.Factory.CreateClassLibrarySpecificBigMetadataWriterStep(_context).Write();
			}
		}
	}
}
