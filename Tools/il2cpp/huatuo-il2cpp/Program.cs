using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using il2cpp.Conversion;
using Mono.Cecil;
using NiceIO;
using Unity.IL2CPP.AssemblyConversion.Phases;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.GenericsCollection;

namespace il2cpp
{
    public class Program
    {
        public static int Main(string[] args)
        {
            try
            {
                Il2CppOptionParser.ParseArguments(args, out var continueToRun, out var exitCode, out var platform,
                    out var buildingOptions, out var profilerOutput);

                var inputData = ContextDataFactory.CreateConversionDataFromOptions();

                using (var context = AssemblyConversionContext.SetupNew(
                           inputData,
                           ContextDataFactory.CreateConversionParametersFromOptions(),
                           ContextDataFactory.CreateTopLevelDataFromOptions()))
                {
                    InitializePhase.Run(context);
                    SetupPhase.Run(context, context.Results.Initialize.AllAssembliesOrderedByDependency);
                    var primaryCollectionContext = context.GlobalPrimaryCollectionContext.CreateCollectionContext();

                    var assemblyDefinitions = new List<AssemblyDefinition>();
                    
                    foreach (var assemblyDefinition in context.Results.Initialize.AllAssembliesOrderedByDependency)
                        if (inputData.Assemblies.Contains(new NPath(assemblyDefinition.MainModule.FileName)))
                            assemblyDefinitions.Add(assemblyDefinition);

                    var inflatedCollectionCollector = GenericsCollector.Collect(primaryCollectionContext,
                        new ReadOnlyCollection<AssemblyDefinition>(assemblyDefinitions));

                    foreach (var type in inflatedCollectionCollector.AsReadOnly().Types)
                        if (type.Scope is ModuleDefinition md && !inputData.Assemblies.Contains(new NPath(md.FileName)))
                            Console.WriteLine(type);

                    return 0;
                }
            }
            catch (Exception arg)
            {
                Console.Error.WriteLine($"Unhandled exception: {arg}");
                return -1;
            }
        }
    }
}