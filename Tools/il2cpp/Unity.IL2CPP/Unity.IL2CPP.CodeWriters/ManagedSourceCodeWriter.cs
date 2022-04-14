using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NiceIO;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Collectors;

namespace Unity.IL2CPP.CodeWriters
{
	public class ManagedSourceCodeWriter : InMemoryGeneratedMethodCodeWriter
	{
		private readonly IDisposable _profilerSection;

		private readonly NPath _filename;

		private readonly IVirtualCallCollector _virtualCallCollector;

		private readonly IMetadataUsageCollectorWriterService _metadataUsage;

		public override NPath FileName => _filename;

		public ManagedSourceCodeWriter(SourceWritingContext context, NPath filename, IDisposable profilerSection)
			: base(context)
		{
			if (filename.HasExtension("h", "hh", "hpp"))
			{
				throw new InvalidOperationException("SourceCodeWriter can only be used to write source files");
			}
			context.Global.Collectors.Stats.RecordFileWritten(filename);
			_profilerSection = profilerSection;
			_filename = filename;
			_virtualCallCollector = context.Global.Collectors.VirtualCalls;
			_metadataUsage = context.Global.Collectors.MetadataUsage;
		}

		public override void Dispose()
		{
			try
			{
				if (!base.ErrorOccurred)
				{
					_virtualCallCollector.AddRange(base.Context, base.Declarations.VirtualMethods.Select((VirtualMethodDeclarationData virtualMethodDeclarationData) => virtualMethodDeclarationData.Method));
					foreach (KeyValuePair<string, MethodMetadataUsage> methodMetadataUsage in base.MethodMetadataUsages)
					{
						_metadataUsage.Add(methodMetadataUsage.Key, methodMetadataUsage.Value);
					}
					using (StreamWriter streamWriter = new StreamWriter(File.Open(_filename.ToString(), FileMode.Create), Encoding.UTF8))
					{
						SourceCodeWriterUtils.WriteCommonIncludes(streamWriter, _filename);
						CppDeclarationsWriter.Write(base.Context, streamWriter, base.Declarations);
						base.Writer.Flush();
						base.Writer.BaseStream.Seek(0L, SeekOrigin.Begin);
						base.Writer.BaseStream.CopyTo(streamWriter.BaseStream);
						streamWriter.Flush();
					}
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
