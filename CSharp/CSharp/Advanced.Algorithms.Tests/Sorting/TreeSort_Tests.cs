using Advanced.Algorithms.Sorting;

using System;
using System.Linq;

namespace Advanced.Algorithms.Tests.Sorting
{
    
    public class TreeSort_Tests
    {
        private static int[] testArray =
            new int[] { 12, 7, 9, 8, 3, 10, 2, 1, 5, 11, 4, 6, 0 };

        [HuaTuo.NUnit.Framework.Test]
        public void TreeSort_Ascending_Smoke_Test()
        {
            var result = TreeSort<int>.Sort(testArray).ToArray();

            for (int i = 0; i < testArray.Length; i++)
            {
                HuaTuo.NUnit.Framework.Assert.AreEqual(i, result[i]);
            }
        }

        [HuaTuo.NUnit.Framework.Test]
        public void TreeSort_Descending_Smoke_Test()
        {
            var result = TreeSort<int>.Sort(testArray, SortDirection.Descending).ToArray();

            for (int i = 0; i < testArray.Length; i++)
            {
                HuaTuo.NUnit.Framework.Assert.AreEqual(testArray.Length - i - 1, result[i]);
            }
        }

        [HuaTuo.NUnit.Framework.Test]
        public void TreeSort_Ascending_Stress_Test()
        {
            var rnd = new Random();
            var nodeCount = 1000;
            var randomNumbers = Enumerable.Range(1, nodeCount)
                                .OrderBy(x => rnd.Next())
                                .ToList();

            var result = TreeSort<int>.Sort(randomNumbers).ToArray();

            for (int i = 1; i <= nodeCount; i++)
            {
                HuaTuo.NUnit.Framework.Assert.AreEqual(i, result[i - 1]);
            }
        }

        [HuaTuo.NUnit.Framework.Test]
        public void TreeSort_Descending_Stress_Test()
        {
            var rnd = new Random();
            var nodeCount = 1000;
            var randomNumbers = Enumerable.Range(1, nodeCount)
                                .OrderBy(x => rnd.Next())
                                .ToList();

            var result = TreeSort<int>.Sort(randomNumbers, SortDirection.Descending).ToArray();

            for (int i = 0; i < nodeCount; i++)
            {
                HuaTuo.NUnit.Framework.Assert.AreEqual(randomNumbers.Count - i, result[i]);
            }
        }

    }
}
