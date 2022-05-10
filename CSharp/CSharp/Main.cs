using System;
using System.Collections.Generic;
using System.Reflection;
using CSharp_aot;
using NBench.Metrics.GarbageCollection;

namespace CSharp
{
    public struct Person
    {
        private string Name;
        private int Age;
    }


    public class Main
    {
        public static void Entry()
        {
            Console.WriteLine(typeof(RefTypes)); // 防止被裁剪
            // var persons = new List<Person>();
            // var dogs = new List<Dog>();

            var dog = new Dog();
            dog.Eat<Person>();


            var assembly = typeof(Main).Assembly;

            // var types = assembly.GetTypes();
            // foreach (var type in types)
            // {
            //     foreach (var method in type.GetMethods())
            //     {
            //         if (method.GetCustomAttribute<TestAttribute>() != null)
            //         {
            //             Console.WriteLine(method.Name);
            //         }
            //     }
            // }
            //
            // Console.WriteLine(assembly.FullName);
            //
            // new AutoRun(assembly).Execute(new []{"--test=Advanced.Algorithms.Tests.Binary"});
            // new AutoRun(assembly).Execute(new []{"--test=Advanced.Algorithms.Tests.Combinatorics"});
            // new AutoRun(assembly).Execute(new []{"--test=Advanced.Algorithms.Tests.Compression"});
            // new AutoRun(assembly).Execute(new []{"--test=Advanced.Algorithms.Tests.DataStructures.Dictionary_Tests"});
            // new AutoRun(assembly).Execute(new []{"--test=Advanced.Algorithms.Tests.DataStructures.TernarySearchTree_Tests"});
            // new AutoRun(assembly).Execute(new []{"--trace=Verbose", "--workers=1"});
            // NBenchRunner.Run<Main>();
            foreach (var type in typeof(GcMeasurementConfigurator).GetTypeInfo().ImplementedInterfaces)
                Console.WriteLine(type);
        }
    }
}