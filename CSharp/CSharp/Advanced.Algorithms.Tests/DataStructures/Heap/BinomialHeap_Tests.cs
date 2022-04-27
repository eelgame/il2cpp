using Advanced.Algorithms.DataStructures;

using System;
using System.Linq;

namespace Advanced.Algorithms.Tests.DataStructures
{
    
    public class BinomialHeap_Tests
    {

        [HuaTuo.NUnit.Framework.Test]
        public void Min_BinomialHeap_Test()
        {
            int nodeCount = 1000 * 10;

            BinomialHeap<int> minHeap = new BinomialHeap<int>();

            for (int i = 0; i <= nodeCount; i++)
            {
                minHeap.Insert(i);
            }

            for (int i = 0; i <= nodeCount; i++)
            {
                minHeap.UpdateKey(i, i - 1);
            }
            int min = 0;
            for (int i = 0; i <= nodeCount; i++)
            {
                min = minHeap.Extract();
                HuaTuo.NUnit.Framework.Assert.AreEqual(min, i - 1);
            }

            //IEnumerable tests.
            HuaTuo.NUnit.Framework.Assert.AreEqual(minHeap.Count, minHeap.Count());

            var rnd = new Random();
            var testSeries = Enumerable.Range(0, nodeCount - 1).OrderBy(x => rnd.Next()).ToList();

            foreach (var item in testSeries)
            {
                minHeap.Insert(item);
            }

            for (int i = 0; i < testSeries.Count; i++)
            {
                var decremented = testSeries[i] - rnd.Next(0, 1000);
                minHeap.UpdateKey(testSeries[i], decremented);
                testSeries[i] = decremented;
            }

            testSeries.Sort();

            for (int i = 0; i < nodeCount - 2; i++)
            {
                min = minHeap.Extract();
                HuaTuo.NUnit.Framework.Assert.AreEqual(testSeries[i], min);
            }

            //IEnumerable tests.
            HuaTuo.NUnit.Framework.Assert.AreEqual(minHeap.Count, minHeap.Count());
        }


        [HuaTuo.NUnit.Framework.Test]
        public void Max_BinomialHeap_Test()
        {
            int nodeCount = 1000 * 10;

            var tree = new BinomialHeap<int>(SortDirection.Descending);

            for (int i = 0; i <= nodeCount; i++)
            {
                tree.Insert(i);
            }

            for (int i = 0; i <= nodeCount; i++)
            {
                tree.UpdateKey(i, i + 1);
            }
            int max = 0;
            for (int i = nodeCount; i >= 0; i--)
            {
                max = tree.Extract();
                HuaTuo.NUnit.Framework.Assert.AreEqual(max, i + 1);
            }

            //IEnumerable tests.
            HuaTuo.NUnit.Framework.Assert.AreEqual(tree.Count, tree.Count());

            var rnd = new Random();
            var testSeries = Enumerable.Range(0, nodeCount - 1).OrderBy(x => rnd.Next()).ToList();

            foreach (var item in testSeries)
            {
                tree.Insert(item);
            }

            for (int i = 0; i < testSeries.Count; i++)
            {
                var incremented = testSeries[i] + rnd.Next(0, 1000);
                tree.UpdateKey(testSeries[i], incremented);
                testSeries[i] = incremented;
            }

            testSeries = testSeries.OrderByDescending(x => x).ToList();

            for (int i = 0; i < nodeCount - 2; i++)
            {
                max = tree.Extract();
                HuaTuo.NUnit.Framework.Assert.AreEqual(testSeries[i], max);
            }

            //IEnumerable tests.
            HuaTuo.NUnit.Framework.Assert.AreEqual(tree.Count, tree.Count());

        }
    }
}
