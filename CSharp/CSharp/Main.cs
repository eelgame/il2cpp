﻿using System;
using CSharp_aot;
using HuaTuo.NUnitLite;

namespace CSharp
{
    public class Main
    {
        public static void Entry()
        {
            Console.WriteLine(typeof(RefTypes)); // 防止被裁剪
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
            new AutoRun(assembly).Execute(new []{"--trace=Verbose", "--workers=1"});
        }
    }
}