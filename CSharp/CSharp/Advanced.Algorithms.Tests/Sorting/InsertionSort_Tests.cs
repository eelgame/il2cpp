using Advanced.Algorithms.Sorting;

using System;
using System.Linq;

namespace Advanced.Algorithms.Tests.Sorting
{
    
    public class InsertionSort_Tests
    {
        private static int[] testArray =
            new int[] { 12, 7, 9, 8, 3, 10, 2, 1, 5, 11, 4, 6, 0 };

        [HuaTuo.NUnit.Framework.Test]
        public void InsertionSort_Ascending_Smoke_Test()
        {
            var result = InsertionSort<int>.Sort(testArray);

            for (int i = 0; i < testArray.Length; i++)
            {
                HuaTuo.NUnit.Framework.Assert.AreEqual(i, result[i]);
            }
        }

        [HuaTuo.NUnit.Framework.Test]
        public void InsertionSort_Descending_Smoke_Test()
        {
            var result = InsertionSort<int>.Sort(testArray, SortDirection.Descending);

            for (int i = 0; i < testArray.Length; i++)
            {
                HuaTuo.NUnit.Framework.Assert.AreEqual(testArray.Length - i - 1, result[i]);
            }
        }

        [HuaTuo.NUnit.Framework.Test]
        public void InsertionSort_Ascending_Stress_Test()
        {
            var rnd = new Random();
            var nodeCount = 1000;
            var randomNumbers = Enumerable.Range(1, nodeCount)
                                .OrderBy(x => rnd.Next())
                                .ToList();

            var result = InsertionSort<int>.Sort(randomNumbers.ToArray());

            for (int i = 1; i <= nodeCount; i++)
            {
                HuaTuo.NUnit.Framework.Assert.AreEqual(i, result[i - 1]);
            }
        }

        [HuaTuo.NUnit.Framework.Test]
        public void InsertionSort_Descending_Stress_Test()
        {
            var rnd = new Random();
            var nodeCount = 1000;
            var randomNumbers = Enumerable.Range(1, nodeCount)
                                .OrderBy(x => rnd.Next())
                                .ToList();

            var result = InsertionSort<int>.Sort(randomNumbers.ToArray(), SortDirection.Descending);

            for (int i = 0; i < nodeCount; i++)
            {
                HuaTuo.NUnit.Framework.Assert.AreEqual(randomNumbers.Count - i, result[i]);
            }
        }

    }
}
