using Advanced.Algorithms.DataStructures.Foundation;

using System;
using System.Linq;

namespace Advanced.Algorithms.Tests.DataStructures
{
    
    public class OrderedHashSet_Tests
    {
        /// <summary>
        /// key value HashSet tests 
        /// </summary>
        [HuaTuo.NUnit.Framework.Test]
        public void OrderedHashSet_Test()
        {
            var hashSet = new OrderedHashSet<int>();

            int nodeCount = 1000;

            //insert test
            for (int i = 0; i <= nodeCount; i++)
            {
                hashSet.Add(i);
                HuaTuo.NUnit.Framework.Assert.AreEqual(true, hashSet.Contains(i));
            }

            //IEnumerable test using linq
            HuaTuo.NUnit.Framework.Assert.AreEqual(hashSet.Count, hashSet.Count());
            HuaTuo.NUnit.Framework.Assert.AreEqual(hashSet.Count, hashSet.AsEnumerableDesc().Count());

            for (int i = 0; i <= nodeCount; i++)
            {
                hashSet.Remove(i);
                HuaTuo.NUnit.Framework.Assert.AreEqual(false, hashSet.Contains(i));
            }

            //IEnumerable test using linq
            HuaTuo.NUnit.Framework.Assert.AreEqual(hashSet.Count, hashSet.Count());
            HuaTuo.NUnit.Framework.Assert.AreEqual(hashSet.Count, hashSet.AsEnumerableDesc().Count());

            var rnd = new Random();
            var testSeries = Enumerable.Range(1, nodeCount).OrderBy(x => rnd.Next()).ToList();

            foreach (var item in testSeries)
            {
                hashSet.Add(item);
                HuaTuo.NUnit.Framework.Assert.AreEqual(true, hashSet.Contains(item));
            }

            //IEnumerable test using linq
            HuaTuo.NUnit.Framework.Assert.AreEqual(hashSet.Count, hashSet.Count());
            HuaTuo.NUnit.Framework.Assert.AreEqual(hashSet.Count, hashSet.AsEnumerableDesc().Count());

            for (int i = 1; i <= nodeCount; i++)
            {
                hashSet.Remove(i);
                HuaTuo.NUnit.Framework.Assert.AreEqual(false, hashSet.Contains(i));
            }

            //IEnumerable test using linq
            HuaTuo.NUnit.Framework.Assert.AreEqual(hashSet.Count, hashSet.Count());
            HuaTuo.NUnit.Framework.Assert.AreEqual(hashSet.Count, hashSet.AsEnumerableDesc().Count());
        }
    }
}
