using Advanced.Algorithms.DataStructures;

using System;
using System.Linq;

namespace Advanced.Algorithms.Tests.DataStructures
{
    
    public class SparseSet_Tests
    {
        [HuaTuo.NUnit.Framework.Test]
        public void SparseSet_Smoke_Test()
        {
            var set = new SparseSet(15, 10);

            set.Add(6);
            set.Add(15);
            set.Add(0);

            //IEnumerable test
            HuaTuo.NUnit.Framework.Assert.AreEqual(set.Count, set.Count());

            set.Remove(15);

            HuaTuo.NUnit.Framework.Assert.IsTrue(set.HasItem(6));
            HuaTuo.NUnit.Framework.Assert.AreEqual(2, set.Count);

            //IEnumerable test
            HuaTuo.NUnit.Framework.Assert.AreEqual(set.Count, set.Count());
        }

        [HuaTuo.NUnit.Framework.Test]
        public void SparseSet_Stress_Test()
        {
            var set = new SparseSet(1000, 1000);

            var random = new Random();
            var testCollection = Enumerable.Range(0, 1000)
                .OrderBy(x => random.Next())
                .ToList();

            foreach (var element in testCollection)
            {
                set.Add(element);
            }

            //IEnumerable test
            HuaTuo.NUnit.Framework.Assert.AreEqual(set.Count, set.Count());

            foreach (var element in testCollection)
            {
                HuaTuo.NUnit.Framework.Assert.IsTrue(set.HasItem(element));
            }

            foreach (var element in testCollection)
            {
                HuaTuo.NUnit.Framework.Assert.IsTrue(set.HasItem(element));
                set.Remove(element);
                HuaTuo.NUnit.Framework.Assert.IsFalse(set.HasItem(element));
            }

            //IEnumerable test
            HuaTuo.NUnit.Framework.Assert.AreEqual(set.Count, set.Count());
        }
    }
}
