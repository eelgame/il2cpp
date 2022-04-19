using System;
using System.Collections.Generic;

namespace CSharp_aot
{
    public static class RefTypes
    {
        static RefTypes()
        {
            var list = new List<object>();
            list.Add(new List<long>());
            Console.WriteLine(list.Count);
        }
    }
}