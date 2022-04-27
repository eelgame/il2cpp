using Advanced.Algorithms.DataStructures.Foundation;

using System;
using System.Linq;

namespace Advanced.Algorithms.Tests.DataStructures
{
    
    public class ArrayList_Tests
    {
        [HuaTuo.NUnit.Framework.Test]
        public void ArrayList_Test()
        {
            var arrayList = new ArrayList<int>();
            int nodeCount = 1000;

            for (int i = 0; i <= nodeCount; i++)
            {
                arrayList.Add(i);
                HuaTuo.NUnit.Framework.Assert.AreEqual(true, arrayList.Contains(i));
            }

            //IEnumerable test using linq
            HuaTuo.NUnit.Framework.Assert.AreEqual(arrayList.Length, arrayList.Count());

            for (int i = 0; i <= nodeCount; i++)
            {
                arrayList.RemoveAt(0);
                HuaTuo.NUnit.Framework.Assert.AreEqual(false, arrayList.Contains(i));
            }

            var rnd = new Random();
            var testSeries = Enumerable.Range(1, nodeCount).OrderBy(x => rnd.Next()).ToList();

            foreach (var item in testSeries)
            {
                arrayList.Add(item);
                HuaTuo.NUnit.Framework.Assert.AreEqual(true, arrayList.Contains(item));
            }

            for (int i = 1; i <= nodeCount; i++)
            {
                arrayList.RemoveAt(0);
            }

        }

        [HuaTuo.NUnit.Framework.Test]
        public void ArrayList_InsertAt_Test()
        {
            var arrayList = new ArrayList<int>();
            int nodeCount = 10;

            for (int i = 0; i <= nodeCount; i++)
            {
                arrayList.InsertAt(i, i);
                HuaTuo.NUnit.Framework.Assert.AreEqual(true, arrayList.Contains(i));
            }

            arrayList.InsertAt(5, 50000);

            //IEnumerable test using linq
            HuaTuo.NUnit.Framework.Assert.AreEqual(arrayList.Length, arrayList.Count());

            HuaTuo.NUnit.Framework.Assert.AreEqual(true, arrayList.Contains(50000));
            HuaTuo.NUnit.Framework.Assert.AreEqual(nodeCount + 2, arrayList.Length);
        }
    }
}
