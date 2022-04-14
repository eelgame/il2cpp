using System.IO;
using NiceIO;

namespace Unity.IL2CPP.CodeWriters
{
	internal static class SourceCodeWriterUtils
	{
		public static void WriteCommonIncludes(StreamWriter writer, NPath fileName)
		{
			if (fileName.ExtensionWithDot.Equals(".cpp"))
			{
				writer.WriteLine("#include \"pch-cpp.hpp\"\n");
			}
			if (fileName.ExtensionWithDot.Equals(".c"))
			{
				writer.WriteLine("#include \"pch-c.h\"");
			}
			writer.WriteLine("#ifndef _MSC_VER");
			writer.WriteLine("# include <alloca.h>");
			writer.WriteLine("#else");
			writer.WriteLine("# include <malloc.h>");
			writer.WriteLine("#endif");
		}
	}
}
