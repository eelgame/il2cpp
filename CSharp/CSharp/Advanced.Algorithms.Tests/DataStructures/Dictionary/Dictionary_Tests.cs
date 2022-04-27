using Advanced.Algorithms.DataStructures.Foundation;

using System;
using System.Linq;

namespace Advanced.Algorithms.Tests.DataStructures
{
    
    public class Dictionary_Tests
    {
        /// <summary>
        /// key value dictionary tests 
        /// </summary>
        [HuaTuo.NUnit.Framework.Test]
        public void Dictionary_SeparateChaining_Test()
        {
            var dictionary = new Dictionary<int, int>(DictionaryType.SeparateChaining);
            int nodeCount = 1000;

            //insert test
            for (int i = 0; i <= nodeCount; i++)
            {
                dictionary.Add(i, i);
                HuaTuo.NUnit.Framework.Assert.AreEqual(true, dictionary.ContainsKey(i));
            }

            //IEnumerable test using linq
            HuaTuo.NUnit.Framework.Assert.AreEqual(dictionary.Count, dictionary.Count());

            for (int i = 0; i <= nodeCount; i++)
            {
                dictionary.Remove(i);
                HuaTuo.NUnit.Framework.Assert.AreEqual(false, dictionary.ContainsKey(i));
            }

            //IEnumerable test using linq
            HuaTuo.NUnit.Framework.Assert.AreEqual(dictionary.Count, dictionary.Count());

            var rnd = new Random();
            var testSeries = Enumerable.Range(1, nodeCount).OrderBy(x => rnd.Next()).ToList();

            foreach (var item in testSeries)
            {
                dictionary.Add(item, item);
                HuaTuo.NUnit.Framework.Assert.AreEqual(true, dictionary.ContainsKey(item));
            }

            //IEnumerable test using linq
            HuaTuo.NUnit.Framework.Assert.AreEqual(dictionary.Count, dictionary.Count());

            foreach (var item in testSeries)
            {
                HuaTuo.NUnit.Framework.Assert.AreEqual(true, dictionary.ContainsKey(item));
            }

            for (int i = 1; i <= nodeCount; i++)
            {
                dictionary.Remove(i);
                HuaTuo.NUnit.Framework.Assert.AreEqual(false, dictionary.ContainsKey(i));
            }

            //IEnumerable test using linq
            HuaTuo.NUnit.Framework.Assert.AreEqual(dictionary.Count, dictionary.Count());

        }


        [HuaTuo.NUnit.Framework.Test]
        public void Dictionary_OpenAddressing_Test()
        {
            var dictionary = new Dictionary<int, int>(DictionaryType.OpenAddressing);
            int nodeCount = 1000;

            //insert test
            for (int i = 0; i <= nodeCount; i++)
            {
                dictionary.Add(i, i);
                HuaTuo.NUnit.Framework.Assert.AreEqual(true, dictionary.ContainsKey(i));
            }

            //IEnumerable test using linq
            HuaTuo.NUnit.Framework.Assert.AreEqual(dictionary.Count, dictionary.Count());

            for (int i = 0; i <= nodeCount; i++)
            {
                dictionary.Remove(i);
                HuaTuo.NUnit.Framework.Assert.AreEqual(false, dictionary.ContainsKey(i));
            }

            //IEnumerable test using linq
            HuaTuo.NUnit.Framework.Assert.AreEqual(dictionary.Count, dictionary.Count());

            var rnd = new Random();
            var testSeries = Enumerable.Range(1, nodeCount).OrderBy(x => rnd.Next()).ToList();

            foreach (var item in testSeries)
            {
                dictionary.Add(item, item);
                HuaTuo.NUnit.Framework.Assert.AreEqual(true, dictionary.ContainsKey(item));
            }

            //IEnumerable test using linq
            HuaTuo.NUnit.Framework.Assert.AreEqual(dictionary.Count, dictionary.Count());

            foreach (var item in testSeries)
            {
                HuaTuo.NUnit.Framework.Assert.AreEqual(true, dictionary.ContainsKey(item));
            }

            for (int i = 1; i <= nodeCount; i++)
            {
                dictionary.Remove(i);
                HuaTuo.NUnit.Framework.Assert.AreEqual(false, dictionary.ContainsKey(i));
            }

            //IEnumerable test using linq
            HuaTuo.NUnit.Framework.Assert.AreEqual(dictionary.Count, dictionary.Count());

        }
    }
}
