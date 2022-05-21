using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using DotNetDetour;
using Mono.Cecil;
using Newtonsoft.Json;
using Unity.IL2CPP.AssemblyConversion;
using Unity.IL2CPP.AssemblyConversion.InProcessPerAssembly;
using Unity.IL2CPP.AssemblyConversion.PerAssembly.Master;
using Unity.IL2CPP.AssemblyConversion.PerAssembly.Slave;
using Unity.IL2CPP.AssemblyConversion.Phases;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.GenericsCollection;
using Unity.IL2CPP.Options;

namespace il2cpp.Huatuo
{
    public class ClassicConverter : BaseAssemblyConverter
    {
        public override void Run(AssemblyConversionContext context)
        {
            Console.WriteLine("il2cpp.Huatuo.ClassicConverter");
            InitializePhase.Run(context);
            SetupPhase.Run(context, context.Results.Initialize.AllAssembliesOrderedByDependency);
            PrimaryCollectionPhase.Run(context, context.Results.Initialize.AllAssembliesOrderedByDependency);
            
            
            
            PrimaryWritePhase.Run(context, context.Results.Initialize.AllAssembliesOrderedByDependency,
                context.Results.Initialize.EntryAssembly);
            SecondaryCollectionPhase.Run(context, context.Results.Initialize.AllAssembliesOrderedByDependency);
            SecondaryWritePhase.Run(context, context.Results.Initialize.AllAssembliesOrderedByDependency);
            MetadataWritePhase.Run(context);
            CompletionPhase.Run(context);
        }
    }

    internal class Hook : IMethodHook
    {
        [HookMethod(typeof(BaseAssemblyConverter))]
        public static BaseAssemblyConverter CreateFor(ConversionMode mode)
        {
            switch (mode)
            {
                case ConversionMode.Default:
                case ConversionMode.Classic:
                    return new ClassicConverter();
                case ConversionMode.PerAssemblySlave:
                    return new SlaveConverter();
                case ConversionMode.PerAssemblyMaster:
                    return new MasterConverter();
                case ConversionMode.PartialPerAssemblyInProcess:
                    return new PartialConverter();
                case ConversionMode.FullPerAssemblyInProcess:
                    return new FullConverter();
                default:
                    throw new ArgumentException(string.Format("Unhandled value of {0}", mode));
            }
        }

        [HookMethod(typeof(Unity.IL2CPP.AssemblyConversion.PrimaryCollection.Steps.Global.GenericsCollection))]
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public static void AddExtraTypes(PrimaryCollectionContext context,
            InflatedCollectionCollector genericsCollectionCollector, ReadOnlyCollection<AssemblyDefinition> assemblies)
        {
            Console.WriteLine("AddExtraTypes");
            foreach (var assembly in assemblies)
            {
                if (assembly.MainModule.Name == "Assembly-CSharp-firstpass.dll")
                {
                    var file = Path.Combine(Path.GetDirectoryName(assembly.MainModule.FileName), "type_mapping.json");
                    if (File.Exists(file))
                    {
                        var mapping = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(file));

                        var assemblyCsharpDll = "Assembly-CSharp.dll";
                        foreach (var type in genericsCollectionCollector.Types.Items.ToArray())
                        {
                            if (type.Scope.Name == assemblyCsharpDll)
                            {
                                continue;
                            }
                            var genericInstanceType = new GenericInstanceType(type.ElementType);
                            bool flag = false;
                            foreach (var genericArgument in type.GenericArguments)
                            {
                                if (genericArgument.Scope.Name == assemblyCsharpDll && genericArgument.IsValueType)
                                {
                                    if (mapping.TryGetValue(genericArgument.FullName, out var typeStr))
                                    {
                                        flag = true;
                                        genericInstanceType.GenericArguments.Add(assembly.MainModule.GetType(typeStr));
                                    }
                                    else
                                    {
                                        genericInstanceType.GenericArguments.Add(genericArgument);
                                    }

                                }
                                else
                                {
                                    genericInstanceType.GenericArguments.Add(genericArgument);
                                }
                            }

                            if (flag)
                            {
                                // Console.WriteLine(genericInstanceType);
                                genericsCollectionCollector.Types.Add(genericInstanceType);
                            }

                        }

                        foreach (var method in genericsCollectionCollector.Methods.Items.ToArray())
                        {
                            if (method.DeclaringType.Scope.Name == assemblyCsharpDll)
                            {
                                continue;
                            }
                            var genericInstanceMethod = new GenericInstanceMethod(method.ElementMethod);
                            bool flag = false;
                            
                            foreach (var genericArgument in method.GenericArguments)
                            {
                                
                                if (genericArgument.Scope.Name == assemblyCsharpDll && genericArgument.IsValueType)
                                {
                                    if (mapping.TryGetValue(genericArgument.FullName, out var typeStr))
                                    {
                                        flag = true;
                                        genericInstanceMethod.GenericArguments.Add(assembly.MainModule.GetType(typeStr));
                                    }
                                    else
                                    {
                                        genericInstanceMethod.GenericArguments.Add(genericArgument);
                                    }

                                }
                                else
                                {
                                    genericInstanceMethod.GenericArguments.Add(genericArgument);
                                }
                            }

                            if (flag)
                            {
                                // Console.WriteLine(genericInstanceMethod);
                                genericsCollectionCollector.Methods.Add(genericInstanceMethod);
                            }

                        }
                    }
                    break;
                }
            }
        }

    }
}