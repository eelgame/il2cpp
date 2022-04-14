using System;
using System.Diagnostics;
using Unity.IL2CPP.Contexts;
using Unity.MiniProfiling;

namespace Unity.IL2CPP.AssemblyConversion
{
	public static class AssemblyConverter
	{
		public static ConversionResults ConvertAssemblies(AssemblyConversionInputData data, AssemblyConversionParameters parameters, AssemblyConversionInputDataForTopLevelAccess dataForTopLevel)
		{
			using (MiniProfiler.Section("ConvertAssemblies"))
			{
				using (AssemblyConversionContext assemblyConversionContext = AssemblyConversionContext.SetupNew(data, parameters, dataForTopLevel))
				{
					try
					{
						Stopwatch stopwatch = new Stopwatch();
						stopwatch.Start();
						try
						{
							BaseAssemblyConverter.CreateFor(dataForTopLevel.ConversionMode).Run(assemblyConversionContext);
						}
						finally
						{
							stopwatch.Stop();
							assemblyConversionContext.Collectors.Stats.ConversionMilliseconds = stopwatch.ElapsedMilliseconds;
							assemblyConversionContext.Collectors.Stats.JobCount = assemblyConversionContext.InputData.JobCount;
						}
						return new ConversionResults(assemblyConversionContext.InputData.Assemblies, assemblyConversionContext.Results.Completion.Stats, assemblyConversionContext.Results.Completion.MatchedAssemblyMethodSourceFiles, assemblyConversionContext.Results.Completion.LoggedMessages);
					}
					catch (AggregateErrorInformationAlreadyProcessedException)
					{
						throw;
					}
					catch (Exception exception)
					{
						throw ErrorMessageWriter.FormatException(assemblyConversionContext.StatefulServices.ErrorInformation, exception);
					}
				}
			}
		}
	}
}
