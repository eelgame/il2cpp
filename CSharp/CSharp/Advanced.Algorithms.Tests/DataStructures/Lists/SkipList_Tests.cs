using Advanced.Algorithms.DataStructures;

using System;
using System.Linq;

namespace Advanced.Algorithms.Tests.DataStructures
{
    
    public class SkipList_Tests
    {
        [NUnit.Framework.Test]
        public void SkipList_Test()
        {
            var skipList = new SkipList<int>();

            for (int i = 1; i < 100; i++)
            {
                skipList.Insert(i);
            }

            for (int i = 1; i < 100; i++)
            {
                NUnit.Framework.Assert.AreEqual(i, skipList.Find(i));
            }

            NUnit.Framework.Assert.AreEqual(0, skipList.Find(101));

            for (int i = 1; i < 100; i++)
            {
                skipList.Delete(i);
                NUnit.Framework.Assert.AreEqual(0, skipList.Find(i));
            }

            for (int i = 1; i < 50; i++)
            {
                skipList.Insert(i);
            }

            try
            {
                skipList.Insert(25);
                NUnit.Framework.Assert.Fail("Duplicate insertion allowed.");
            }
            catch (Exception) { }

            try
            {
                skipList.Delete(52);
                NUnit.Framework.Assert.Fail("Deletion of item not in skip list did'nt throw exception.");
            }
            catch (Exception) { }

            //IEnumerable test using linq
            NUnit.Framework.Assert.AreEqual(skipList.Count, skipList.Count());

            for (int i = 1; i < 50; i++)
            {
                NUnit.Framework.Assert.AreEqual(i, skipList.Find(i));
            }

            for (int i = 1; i < 50; i++)
            {
                skipList.Delete(i);
                NUnit.Framework.Assert.AreEqual(0, skipList.Find(i));
            }
        }
    }
}
