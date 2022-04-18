using Advanced.Algorithms.DataStructures;

using System;
using System.Collections.Generic;
using System.Linq;


namespace Advanced.Algorithms.Tests.DataStructures
{
    
    public class BHeap_Tests
    {

        [NUnit.Framework.Test]
        public void Min_BHeap_Test()
        {
            var rnd = new Random();
            var initial = Enumerable.Range(0, 51).OrderBy(x => rnd.Next()).ToList();

            var minHeap = new BHeap<int>(SortDirection.Ascending, initial);

            for (int i = 51; i <= 99; i++)
            {
                minHeap.Insert(i);
            }

            for (int i = 0; i <= 99; i++)
            {
                var min = minHeap.Extract();
                NUnit.Framework.Assert.AreEqual(min, i);
            }

            //IEnumerable tests.
            NUnit.Framework.Assert.AreEqual(minHeap.Count, minHeap.Count());

            var testSeries = Enumerable.Range(1, 49).OrderBy(x => rnd.Next()).ToList();

            foreach (var item in testSeries)
            {
                minHeap.Insert(item);
            }

            for (int i = 1; i <= 49; i++)
            {
                var min = minHeap.Extract();
                NUnit.Framework.Assert.AreEqual(min, i);
            }

            //IEnumerable tests.
            NUnit.Framework.Assert.AreEqual(minHeap.Count, minHeap.Count());

        }


        [NUnit.Framework.Test]
        public void Max_BHeap_Test()
        {
            var rnd = new Random();

            var initial = new List<int>(Enumerable.Range(0, 51)
                .OrderBy(x => rnd.Next()));


            var maxHeap = new BHeap<int>(SortDirection.Descending, initial);

            for (int i = 51; i <= 99; i++)
            {
                maxHeap.Insert(i);
            }

            for (int i = 0; i <= 99; i++)
            {
                var max = maxHeap.Extract();
                NUnit.Framework.Assert.AreEqual(max, 99 - i);
            }

            //IEnumerable tests.
            NUnit.Framework.Assert.AreEqual(maxHeap.Count, maxHeap.Count());

            var testSeries = Enumerable.Range(1, 49)
                .OrderBy(x => rnd.Next()).ToList();

            foreach (var item in testSeries)
            {
                maxHeap.Insert(item);
            }

            for (int i = 1; i <= 49; i++)
            {
                var max = maxHeap.Extract();
                NUnit.Framework.Assert.AreEqual(max, 49 - i + 1);
            }

            //IEnumerable tests.
            NUnit.Framework.Assert.AreEqual(maxHeap.Count, maxHeap.Count());

        }
    }
}
