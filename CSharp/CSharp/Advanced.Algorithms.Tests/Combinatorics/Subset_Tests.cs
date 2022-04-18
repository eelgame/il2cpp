using Advanced.Algorithms.Combinatorics;

using System;
using System.Linq;

namespace Advanced.Algorithms.Tests.Combinatorics
{
    
    public class Subset_Tests
    {

        [NUnit.Framework.Test]
        public void Subset_Smoke_Test()
        {
            var input = "".ToCharArray().ToList();
            var subsets = Subset.Find<char>(input);
            NUnit.Framework.Assert.AreEqual(Math.Pow(2, input.Count), subsets.Count);

            input = "cookie".ToCharArray().ToList();
            subsets = Subset.Find<char>(input);
            NUnit.Framework.Assert.AreEqual(Math.Pow(2, input.Count), subsets.Count);

            input = "monster".ToCharArray().ToList();
            subsets = Subset.Find<char>(input);
            NUnit.Framework.Assert.AreEqual(Math.Pow(2, input.Count), subsets.Count);
        }


    }
}
