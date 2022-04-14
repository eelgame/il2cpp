using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Unity.IL2CPP.AssemblyConversion.Phases;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Options;

namespace Unity.IL2CPP.AssemblyConversion.PerAssembly.Master
{
	internal class MasterConverter : BaseAssemblyConverter
	{
		public override void Run(AssemblyConversionContext context)
		{
			InitializePhase.Run(context);
			PrimaryWritePhase(context);
			MetadataWritePhase.Run(context);
		}

		private static void PrimaryWritePhase(AssemblyConversionContext context)
		{
			ProcessResults(new SlaveDispatcher(BuildSlaveInstanceData(context)).Run());
		}

		private static SlaveInstanceData[] BuildSlaveInstanceData(AssemblyConversionContext context)
		{
			List<SlaveInstanceData> list = new List<SlaveInstanceData>();
			foreach (AssemblyDefinition item in context.Results.Initialize.AllAssembliesOrderedByDependency)
			{
				List<string> list2 = new List<string>
				{
					context.InputDataForTopLevel.FormatArgumentFromCodeGenOptions("ConversionMode", ConversionMode.PerAssemblySlave),
					context.InputDataForTopLevel.FormatArgumentFromCodeGenOptions("SlaveAssembly", item.MainModule.FileName.InQuotes())
				};
				if (System.Diagnostics.Debugger.IsAttached && PlatformUtils.IsWindows())
				{
					list2.Add(context.InputDataForTopLevel.FormatArgumentFromCodeGenOptions("SlaveEnableAttachMessage", item.Name.Name));
				}
				string[] collection = context.InputDataForTopLevel.GetMasterToSlavePassThroughArguments();
				list2.AddRange(collection);
				list.Add(new SlaveInstanceData
				{
					PrimaryAssembly = item,
					Arguments = list2.ToArray()
				});
			}
			return list.ToArray();
		}

		private static void ProcessResults(SlaveInstanceResult[] results)
		{
			SlaveInstanceResult slaveInstanceResult = results.FirstOrDefault((SlaveInstanceResult r) => r.Failed);
			if (slaveInstanceResult != null)
			{
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.AppendLine($"il2cpp slave for assembly `{slaveInstanceResult.Data.PrimaryAssembly.Name.Name}` failed with exit code {slaveInstanceResult.ShellResult.ExitCode}");
				if (!string.IsNullOrWhiteSpace(slaveInstanceResult.ShellResult.StdOut))
				{
					stringBuilder.AppendLine(slaveInstanceResult.ShellResult.StdOut);
				}
				if (!string.IsNullOrWhiteSpace(slaveInstanceResult.ShellResult.StdErr))
				{
					stringBuilder.AppendLine(slaveInstanceResult.ShellResult.StdErr);
				}
				throw new SlaveFailedException(stringBuilder.ToString());
			}
			if (results.Any((SlaveInstanceResult r) => r.Skipped))
			{
				throw new InvalidOperationException("Something went wrong.  There were skipped slaves but no failed slaves");
			}
		}
	}
}
