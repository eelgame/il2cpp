﻿using System;
using System.Globalization;
using System.IO;
using System.Threading;
using DotNetDetour;
using il2cpp;
using il2cpp.Compilation;
using il2cpp.Conversion;
using il2cpp.EditorIntegration;
using Unity.IL2CPP.AssemblyConversion;
using Unity.IL2CPP.Building;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Statistics;
using Unity.MiniProfiling;

public class Program
{
    public static int Main(string[] args)
    {
        try
        {
            MethodHook.Install();
            Console.WriteLine(@" 
__  __                  ______                  
/\ \/\ \                /\__  _\                 
\ \ \_\ \  __  __     __\/_/\ \/ __  __    ___   
 \ \  _  \/\ \/\ \  /'__`\ \ \ \/\ \/\ \  / __`\ 
  \ \ \ \ \ \ \_\ \/\ \L\.\_\ \ \ \ \_\ \/\ \L\ \
   \ \_\ \_\ \____/\ \__/.\_\\ \_\ \____/\ \____/
    \/_/\/_/\/___/  \/__/\/_/ \/_/\/___/  \/___/ ");

            return (int) Run(args, false, false);
        }
        catch (Exception arg)
        {
            Console.Error.WriteLine($"Unhandled exception: {arg}");
            return -1;
        }
    }

    public static ExitCode Run(string[] args, bool setInvariantCulture, bool throwExceptions = true)
    {
        if (setInvariantCulture) Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        Il2CppOptionParser.ParseArguments(args, out var continueToRun, out var exitCode, out var platform,
            out var buildingOptions, out var profilerOutput);
        if (IL2CPPOptions.DebugEnableAttach)
            DebugAttacher.AttachToCurrentProcess(IL2CPPOptions.DebugRiderInstallPath, IL2CPPOptions.DebugSolutionPath);
        if (!continueToRun) return exitCode;
        using (MiniProfiler.Capture(profilerOutput, "il2cpp.exe"))
        {
            return DoRun(args, platform, buildingOptions, throwExceptions);
        }
    }

    private static ExitCode DoRun(string[] args, RuntimePlatform platform, BuildingOptions buildingOptions,
        bool throwExceptions)
    {
        using (var il2CppEditorDataGenerator = new Il2CppEditorDataGenerator(args,
                   IL2CPPOptions.Generatedcppdir ?? Directory.GetCurrentDirectory()))
        {
            try
            {
                ConversionResults conversionResults = null;
                CompilationResults compilationResults = null;
                if (IL2CPPOptions.ConvertToCpp) conversionResults = ConversionDriver.Run();
                if (IL2CPPOptions.CompileCpp || BuildingOptionsParser.GenerateCMakeEnabled)
                    compilationResults = CompilationDriver.Run(platform, buildingOptions,
                        conversionResults?.MatchedAssemblyMethodSourceFiles);
                if (CodeGenOptions.EnableStats)
                {
                    StatisticsGenerator.WriteStatsLog(ConsoleOutput.Info.Stdout, conversionResults?.Stats,
                        compilationResults?.Statistics);
                    StatisticsGenerator.Generate(StatisticsOptions.StatsOutputDir, IL2CPPOptions.Generatedcppdir,
                        conversionResults?.Stats, compilationResults?.Statistics, ProfilerSnapshot.Capture(), args,
                        conversionResults?.ConvertedAssemblies);
                }

                if (conversionResults?.LoggedMessages != null)
                    il2CppEditorDataGenerator.LogFromMessages(conversionResults.LoggedMessages);
            }
            catch (Exception ex)
            {
                il2CppEditorDataGenerator.LogException(ex);
                if (throwExceptions) throw;
                return ExceptionToExitCode(ex);
            }
        }

        return ExitCode.Success;
    }

    private static ExitCode ExceptionToExitCode(Exception ex)
    {
        if (!(ex is UserMessageException))
        {
            if (!(ex is PathTooLongException))
            {
                if (ex is BuilderFailedException) return ExitCode.BuilderError;
                return ExitCode.UnexpectedError;
            }

            return ExitCode.PathTooLong;
        }

        return ExitCode.UserErrorMessage;
    }
}