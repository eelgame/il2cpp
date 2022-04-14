using System;
using System.Collections.ObjectModel;
using System.Linq;
using Mono.Cecil;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts;
using Unity.MiniProfiling;

namespace Unity.IL2CPP.AssemblyConversion.Phases
{
	public static class InitializePhase
	{
		public static void Run(AssemblyConversionContext context)
		{
			using (MiniProfiler.Section("InitializePhase"))
			{
				ReadOnlyCollection<AssemblyDefinition> readOnlyCollection;
				using (MiniProfiler.Section("Collect assemblies to convert"))
				{
					readOnlyCollection = AssemblyCollector.CollectAssembliesToConvert(context.InputData.Assemblies.ToArray(), context.Services.AssemblyLoader, context.Services.AssemblyDependencies.InternalComponent);
				}
				AssemblyDefinition entryAssembly = GetEntryAssembly(context, readOnlyCollection);
				context.Results.SetInitializePhaseResults(new AssemblyConversionResults.InitializePhase(readOnlyCollection, entryAssembly));
			}
		}

		public static AssemblyDefinition GetEntryAssembly(AssemblyConversionContext context, ReadOnlyCollection<AssemblyDefinition> assemblies)
		{
			AssemblyDefinition[] array = assemblies.Where((AssemblyDefinition a) => a.MainModule.Kind == ModuleKind.Windows || a.MainModule.Kind == ModuleKind.Console).ToArray();
			if (array.Length == 1)
			{
				return array[0];
			}
			AssemblyDefinition assemblyDefinition = null;
			if (array.Length != 0)
			{
				if (string.IsNullOrEmpty(context.InputData.EntryAssemblyName))
				{
					assemblyDefinition = array.FirstOrDefault((AssemblyDefinition a) => a.EntryPoint != null);
					if (assemblyDefinition != null)
					{
						return assemblyDefinition;
					}
				}
				assemblyDefinition = array.FirstOrDefault((AssemblyDefinition a) => a.Name.Name == context.InputData.EntryAssemblyName);
				if (assemblyDefinition == null)
				{
					string text = "An entry assembly name of '" + context.InputData.EntryAssemblyName + "' was provided via the command line option --entry-assembly-name, but no assemblies were found with that name." + Environment.NewLine + "Here are the assemblies we looked in:" + Environment.NewLine;
					AssemblyDefinition[] array2 = array;
					foreach (AssemblyDefinition assemblyDefinition2 in array2)
					{
						text = text + "\t" + assemblyDefinition2.FullName + Environment.NewLine;
					}
					text += "Note that the entry assembly name should _not_ have a file name extension.";
					throw new InvalidOperationException(text);
				}
			}
			return assemblyDefinition;
		}
	}
}
