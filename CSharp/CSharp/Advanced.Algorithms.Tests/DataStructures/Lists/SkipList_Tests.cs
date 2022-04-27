using Advanced.Algorithms.DataStructures;

using System;
using System.Linq;

namespace Advanced.Algorithms.Tests.DataStructures
{
    
    public class SkipList_Tests
    {
        [HuaTuo.NUnit.Framework.Test]
        public void SkipList_Test()
        {
            var skipList = new SkipList<int>();

            for (int i = 1; i < 100; i++)
            {
                skipList.Insert(i);
            }

            for (int i = 1; i < 100; i++)
            {
                HuaTuo.NUnit.Framework.Assert.AreEqual(i, skipList.Find(i));
            }

            HuaTuo.NUnit.Framework.Assert.AreEqual(0, skipList.Find(101));

            for (int i = 1; i < 100; i++)
            {
                skipList.Delete(i);
                HuaTuo.NUnit.Framework.Assert.AreEqual(0, skipList.Find(i));
            }

            for (int i = 1; i < 50; i++)
            {
                skipList.Insert(i);
            }

            try
            {
                skipList.Insert(25);
                HuaTuo.NUnit.Framework.Assert.Fail("Duplicate insertion allowed.");
            }
            catch (Exception) { }

            try
            {
                skipList.Delete(52);
                HuaTuo.NUnit.Framework.Assert.Fail("Deletion of item not in skip list did'nt throw exception.");
            }
            catch (Exception) { }

            //IEnumerable test using linq
            HuaTuo.NUnit.Framework.Assert.AreEqual(skipList.Count, skipList.Count());

            for (int i = 1; i < 50; i++)
            {
                HuaTuo.NUnit.Framework.Assert.AreEqual(i, skipList.Find(i));
            }

            for (int i = 1; i < 50; i++)
            {
                skipList.Delete(i);
                HuaTuo.NUnit.Framework.Assert.AreEqual(0, skipList.Find(i));
            }
        }
    }
}
