using System;
using NUnitLite;

namespace CSharp
{
    public class Main
    {
        public static void Entry()
        {
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

            new AutoRun(assembly).Execute(Array.Empty<string>());
        }
    }
}