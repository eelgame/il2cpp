using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using NiceIO;
using Unity.Cecil.Awesome;
using Unity.IL2CPP.AssemblyConversion;

namespace Unity.IL2CPP.Contexts.Components
{
	public class AssemblyLoaderComponent : IAssemblyLoader, IDisposable
	{
		private readonly AssemblyLoader _assemblyLoader;

		public AssemblyLoaderComponent(AssemblyConversionInputData inputData)
		{
			List<NPath> list = new List<NPath>();
			if (inputData.Assemblies != null && inputData.Assemblies.Count > 0)
			{
				list.AddRange(inputData.Assemblies.Select((NPath asm) => asm.Parent).Distinct());
			}
			if (inputData.SearchDirectories != null)
			{
				list.AddRange(inputData.SearchDirectories);
			}
			bool supportsWindowsRuntime = inputData.Profile.SupportsWindowsRuntime;
			_assemblyLoader = new AssemblyLoader(list.ToStringPaths(), supportsWindowsRuntime, aggregateWindowsMetadata: true, inputData.CecilReadingMode, inputData.DebugAssemblyName);
		}

		public AssemblyDefinition Load(string name)
		{
			return _assemblyLoader.Load(name);
		}

		public AssemblyDefinition Resolve(IMetadataScope scope)
		{
			return _assemblyLoader.Resolve(scope);
		}

		public void CacheAssembly(AssemblyDefinition assembly)
		{
			_assemblyLoader.CacheAssembly(assembly);
		}

		public void Dispose()
		{
			_assemblyLoader.Dispose();
		}
	}
}
