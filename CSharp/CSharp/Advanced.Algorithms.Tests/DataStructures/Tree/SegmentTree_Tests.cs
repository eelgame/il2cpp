using Advanced.Algorithms.DataStructures;

using System;

namespace Advanced.Algorithms.Tests.DataStructures
{
    
    public class SegmentTreeTests
    {
        /// <summary>
        /// Smoke test
        /// </summary>
        [HuaTuo.NUnit.Framework.Test]
        public void SegmentTree_Sum_Smoke_Test()
        {
            var testArray = new int[] { 1, 3, 5, 7, 9, 11 };

            //tree with sum operation
            var tree = new SegmentTree<int>(testArray,
                new Func<int, int, int>((x, y) => x + y),
                new Func<int>(() => 0));

            var sum = tree.RangeResult(1, 3);

            HuaTuo.NUnit.Framework.Assert.AreEqual(15, sum);
        }
    }
}
