using NiceIO;

namespace Unity.IL2CPP.Building.ToolChains.MsvcVersions
{
	internal class VCPaths
	{
		public readonly NPath ToolsPath;

		public readonly NPath IncludePath;

		public readonly NPath LibPath;

		public readonly NPath RedistPath;

		public VCPaths(NPath toolsPath, NPath includePath, NPath libPath, NPath redistPath = null)
		{
			ToolsPath = toolsPath;
			IncludePath = includePath;
			LibPath = libPath;
			RedistPath = redistPath;
		}
	}
}
