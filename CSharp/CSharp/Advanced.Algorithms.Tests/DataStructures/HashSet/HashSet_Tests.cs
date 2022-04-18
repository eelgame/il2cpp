using Advanced.Algorithms.DataStructures.Foundation;

using System;
using System.Linq;

namespace Advanced.Algorithms.Tests.DataStructures
{
    
    public class HashSet_Tests
    {
        /// <summary>
        /// key value dictionary tests 
        /// </summary>
        [NUnit.Framework.Test]
        public void HashSet_SeparateChaining_Test()
        {
            var hashSet = new HashSet<int>(HashSetType.SeparateChaining);
            int nodeCount = 1000;

            //insert test
            for (int i = 0; i <= nodeCount; i++)
            {
                hashSet.Add(i);
                NUnit.Framework.Assert.AreEqual(true, hashSet.Contains(i));
            }

            //IEnumerable test using linq
            NUnit.Framework.Assert.AreEqual(hashSet.Count, hashSet.Count());

            for (int i = 0; i <= nodeCount; i++)
            {
                hashSet.Remove(i);
                NUnit.Framework.Assert.AreEqual(false, hashSet.Contains(i));
            }

            //IEnumerable test using linq
            NUnit.Framework.Assert.AreEqual(hashSet.Count, hashSet.Count());

            var rnd = new Random();
            var testSeries = Enumerable.Range(1, nodeCount).OrderBy(x => rnd.Next()).ToList();

            foreach (var item in testSeries)
            {
                hashSet.Add(item);
                NUnit.Framework.Assert.AreEqual(true, hashSet.Contains(item));
            }

            //IEnumerable test using linq
            NUnit.Framework.Assert.AreEqual(hashSet.Count, hashSet.Count());

            foreach (var item in testSeries)
            {
                NUnit.Framework.Assert.AreEqual(true, hashSet.Contains(item));
            }

            for (int i = 1; i <= nodeCount; i++)
            {
                hashSet.Remove(i);
                NUnit.Framework.Assert.AreEqual(false, hashSet.Contains(i));
            }

            //IEnumerable test using linq
            NUnit.Framework.Assert.AreEqual(hashSet.Count, hashSet.Count());

        }


        [NUnit.Framework.Test]
        public void HashSet_OpenAddressing_Test()
        {
            var hashSet = new HashSet<int>(HashSetType.OpenAddressing);
            int nodeCount = 1000;

            //insert test
            for (int i = 0; i <= nodeCount; i++)
            {
                hashSet.Add(i);
                NUnit.Framework.Assert.AreEqual(true, hashSet.Contains(i));
            }

            //IEnumerable test using linq
            NUnit.Framework.Assert.AreEqual(hashSet.Count, hashSet.Count());

            for (int i = 0; i <= nodeCount; i++)
            {
                hashSet.Remove(i);
                NUnit.Framework.Assert.AreEqual(false, hashSet.Contains(i));
            }

            //IEnumerable test using linq
            NUnit.Framework.Assert.AreEqual(hashSet.Count, hashSet.Count());

            var rnd = new Random();
            var testSeries = Enumerable.Range(1, nodeCount).OrderBy(x => rnd.Next()).ToList();

            foreach (var item in testSeries)
            {
                hashSet.Add(item);
                NUnit.Framework.Assert.AreEqual(true, hashSet.Contains(item));
            }

            //IEnumerable test using linq
            NUnit.Framework.Assert.AreEqual(hashSet.Count, hashSet.Count());

            foreach (var item in testSeries)
            {
                NUnit.Framework.Assert.AreEqual(true, hashSet.Contains(item));
            }

            for (int i = 1; i <= nodeCount; i++)
            {
                hashSet.Remove(i);
                NUnit.Framework.Assert.AreEqual(false, hashSet.Contains(i));
            }

            //IEnumerable test using linq
            NUnit.Framework.Assert.AreEqual(hashSet.Count, hashSet.Count());
        }
    }
}
