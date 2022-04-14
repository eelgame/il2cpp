using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using NiceIO;
using Unity.IL2CPP.Common;
using Unity.MiniProfiling;

namespace Unity.IL2CPP.Building.Hashing
{
	public abstract class FileHashProvider
	{
		private readonly ConcurrentDictionary<string, string> _cache = new ConcurrentDictionary<string, string>();

		public void Initialize(IEnumerable<CppCompilationInstruction> cppSourceCompileInstructions, string[] fileExtensions)
		{
			ParallelFor.Run(cppSourceCompileInstructions.SelectMany((CppCompilationInstruction ins) => from path in DirectoriesToInitializeForInstruction(ins)
				where path.DirectoryExists()
				select path).Distinct().ToSortedCollection()
				.ToArray(), delegate(NPath dir)
			{
				using (MiniProfiler.Section("FileHashProvider.Initialize", dir))
				{
					HashOfAllFilesWithProviderExtensionInDirectory(dir, fileExtensions);
				}
			});
		}

		protected abstract IEnumerable<NPath> DirectoriesToInitializeForInstruction(CppCompilationInstruction ins);

		protected string HashOfAllFilesWithProviderExtensionInDirectory(NPath directory, string[] fileExtensions)
		{
			if (fileExtensions == null || fileExtensions.Length == 0)
			{
				throw new ArgumentException("fileExtensions must have at least 1 or more elements");
			}
			return string.Concat(fileExtensions.Select((string e) => HashOfAllFilesWithProviderExtensionInDirectory(directory, e)));
		}

		private string HashOfAllFilesWithProviderExtensionInDirectory(NPath directory, string extension)
		{
			string key2 = directory.ToString(SlashMode.Forward) + "_" + extension;
			if (_cache.TryGetValue(key2, out var value2))
			{
				return value2;
			}
			using (MiniProfiler.Section("HashOfAllFilesWithProviderExtensionInDirectory", $"{extension} {directory}"))
			{
				string text = string.Concat(ParallelFor.RunWithResult(directory.Files("*" + extension, recurse: true).ToSortedCollection().ToArray(), delegate(NPath f)
				{
					using (MiniProfiler.Section("HashOfFile", f.ToString()))
					{
						return HashTools.HashOfFile(f);
					}
				}, 2));
				_cache.AddOrUpdate(key2, text, (string key, string value) => value);
				return text;
			}
		}
	}
}
