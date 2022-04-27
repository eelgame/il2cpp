using Advanced.Algorithms.DataStructures;

using System;

namespace Advanced.Algorithms.Tests.DataStructures
{
    
    public class FenwickTreeTests
    {
        /// <summary>
        /// Smoke test
        /// </summary>
        [HuaTuo.NUnit.Framework.Test]
        public void FenwickTree_Sum_Smoke_Test()
        {
            var testArray = new int[] { 1, 3, 5, 7, 9, 11 };

            //tree with sum operation
            var tree = new FenwickTree<int>(testArray,
                new Func<int, int, int>((x, y) => x + y));

            var sum = tree.PrefixSum(3);

            HuaTuo.NUnit.Framework.Assert.AreEqual(16, sum);
        }
    }
}
