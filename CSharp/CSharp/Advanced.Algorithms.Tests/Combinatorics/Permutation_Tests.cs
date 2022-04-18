using Advanced.Algorithms.Combinatorics;

using System;
using System.Linq;

namespace Advanced.Algorithms.Tests.Combinatorics
{
    
    public class Permutation_Tests
    {
        //for verification
        static readonly Func<int, int> factorial = n => n == 0 ? 1 :
            Enumerable.Range(1, n).Aggregate((acc, x) => acc * x);

        //for verification
        static readonly Func<int, int, int> permutation = (int n, int r)
            => n == 0 || r == 0 ? 1 : factorial(n) / factorial(n - r);

        [NUnit.Framework.Test]
        public void Permutation_With_Repetition_Smoke_Test()
        {
            var input = "".ToCharArray().ToList();
            var permuations = Permutation.Find<char>(input, input.Count, true);
            NUnit.Framework.Assert.AreEqual(Math.Pow(input.Count, input.Count), permuations.Count);

            input = "pen".ToCharArray().ToList();
            permuations = Permutation.Find<char>(input, input.Count, true);
            NUnit.Framework.Assert.AreEqual(Math.Pow(input.Count, input.Count), permuations.Count);

            input = "scan".ToCharArray().ToList();
            permuations = Permutation.Find<char>(input, input.Count, true);
            NUnit.Framework.Assert.AreEqual(Math.Pow(input.Count, input.Count), permuations.Count);

            input = "scan".ToCharArray().ToList();
            permuations = Permutation.Find<char>(input, 2, true);
            NUnit.Framework.Assert.AreEqual(Math.Pow(input.Count, 2), permuations.Count);

            input = "scan".ToCharArray().ToList();
            permuations = Permutation.Find<char>(input, 3, true);
            NUnit.Framework.Assert.AreEqual(Math.Pow(input.Count, 3), permuations.Count);

            input = "scaner".ToCharArray().ToList();
            permuations = Permutation.Find<char>(input, 4, true);
            NUnit.Framework.Assert.AreEqual(Math.Pow(input.Count, 4), permuations.Count);
        }


        [NUnit.Framework.Test]
        public void Permutation_Without_Repetitions_Smoke_Test()
        {
            var input = "".ToCharArray().ToList();
            var permuations = Permutation.Find<char>(input, input.Count);
            NUnit.Framework.Assert.AreEqual(permutation(input.Count, input.Count), permuations.Count);

            input = "cookie".ToCharArray().ToList();
            permuations = Permutation.Find<char>(input, input.Count);
            NUnit.Framework.Assert.AreEqual(permutation(input.Count, input.Count), permuations.Count);

            input = "monster".ToCharArray().ToList();
            permuations = Permutation.Find<char>(input, input.Count);
            NUnit.Framework.Assert.AreEqual(permutation(input.Count, input.Count), permuations.Count);

            input = "cookie".ToCharArray().ToList();
            permuations = Permutation.Find<char>(input, 2);
            NUnit.Framework.Assert.AreEqual(permutation(input.Count, 2), permuations.Count);

            input = "monster".ToCharArray().ToList();
            permuations = Permutation.Find<char>(input, 3);
            NUnit.Framework.Assert.AreEqual(permutation(input.Count, 3), permuations.Count);
        }
    }
}
