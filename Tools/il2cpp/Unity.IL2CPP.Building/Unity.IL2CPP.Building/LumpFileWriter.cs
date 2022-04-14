using System.Collections.Generic;
using System.IO;
using System.Linq;
using NiceIO;

namespace Unity.IL2CPP.Building
{
	public class LumpFileWriter
	{
		private readonly NPath _lumpedSourceDirectory;

		private readonly List<NPath> _lumpFilesWritten;

		public LumpFileWriter(NPath lumpedSourceDirectory)
		{
			_lumpedSourceDirectory = lumpedSourceDirectory;
			_lumpedSourceDirectory.EnsureDirectoryExists();
			_lumpFilesWritten = new List<NPath>();
		}

		public NPath Write(NPath folderToLump, HashSet<NPath> filesToLump)
		{
			NPath nPath = _lumpedSourceDirectory.Combine("Lump_" + folderToLump.Parent.FileName + "_" + folderToLump.FileName + ".cpp");
			nPath.DeleteIfExists();
			using (StreamWriter streamWriter = new StreamWriter(nPath))
			{
				streamWriter.WriteLine("#include \"il2cpp-config.h\"");
				foreach (NPath item in folderToLump.Files("*.cpp", recurse: true).Where(filesToLump.Contains).ToSortedCollection())
				{
					streamWriter.WriteLine($"#include \"{item}\"");
				}
			}
			_lumpFilesWritten.Add(nPath);
			return nPath;
		}

		public bool IsLumpFile(NPath file)
		{
			return _lumpFilesWritten.Contains(file);
		}
	}
}
