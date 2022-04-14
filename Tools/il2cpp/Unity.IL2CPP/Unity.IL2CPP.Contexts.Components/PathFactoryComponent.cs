using System.IO;
using Mono.Cecil;
using NiceIO;
using Unity.IL2CPP.Contexts.Components.Base;
using Unity.IL2CPP.Contexts.Services;

namespace Unity.IL2CPP.Contexts.Components
{
	public class PathFactoryComponent : ServiceComponentBase<IPathFactoryService, PathFactoryComponent>, IPathFactoryService
	{
		private const string PrefixAndNameSeparator = "_";

		private readonly string _perAssemblyFileNamePrefix;

		public PathFactoryComponent()
		{
			_perAssemblyFileNamePrefix = null;
		}

		public PathFactoryComponent(string perAssemblyFileNamePrefix)
		{
			_perAssemblyFileNamePrefix = perAssemblyFileNamePrefix;
		}

		public static string GenerateFileNamePrefixForAssembly(AssemblyDefinition assembly)
		{
			return Path.GetFileNameWithoutExtension(Path.GetFileName(assembly.MainModule.FileName ?? assembly.MainModule.Name));
		}

		protected override PathFactoryComponent ThisAsFull()
		{
			return this;
		}

		protected override IPathFactoryService ThisAsRead()
		{
			return this;
		}

		public string GetFileName(string fileName)
		{
			if (_perAssemblyFileNamePrefix == null)
			{
				return fileName;
			}
			if (fileName.StartsWith(_perAssemblyFileNamePrefix))
			{
				return fileName;
			}
			return GenerateFileNameWithPreFix(_perAssemblyFileNamePrefix, fileName);
		}

		public string GetFileNameForAssembly(AssemblyDefinition assembly, string fileName)
		{
			return GenerateFileNameWithPreFix(GenerateFileNamePrefixForAssembly(assembly), fileName);
		}

		public NPath GetFilePath(NPath filePath)
		{
			if (_perAssemblyFileNamePrefix == null)
			{
				return filePath;
			}
			if (filePath.FileName.StartsWith(_perAssemblyFileNamePrefix))
			{
				return filePath;
			}
			return filePath.Parent.Combine(GenerateFileNameWithPreFix(_perAssemblyFileNamePrefix, filePath.FileName));
		}

		private static string GenerateFileNameWithPreFix(string prefix, string fileName)
		{
			if (string.IsNullOrEmpty(Path.GetFileNameWithoutExtension(fileName)))
			{
				return prefix + fileName;
			}
			return prefix + "_" + fileName;
		}
	}
}
