using Advanced.Algorithms.Distributed;


namespace Advanced.Algorithms.Tests
{
    
    public class LRUCache_Tests
    {

        [HuaTuo.NUnit.Framework.Test]
        public void LRUCache_Smoke_Test()
        {
            var cache = new LRUCache<int, int>(2);

            cache.Put(1, 1);
            cache.Put(2, 2);
            HuaTuo.NUnit.Framework.Assert.AreEqual(1, cache.Get(1));

            cache.Put(3, 3);
            HuaTuo.NUnit.Framework.Assert.AreEqual(0, cache.Get(2));

            cache.Put(4, 4);
            HuaTuo.NUnit.Framework.Assert.AreEqual(0, cache.Get(1));
            HuaTuo.NUnit.Framework.Assert.AreEqual(3, cache.Get(3));
            HuaTuo.NUnit.Framework.Assert.AreEqual(4, cache.Get(4));
        }

    }
}