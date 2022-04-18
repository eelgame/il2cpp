using Advanced.Algorithms.DataStructures;



namespace Advanced.Algorithms.Tests.DataStructures
{
    
    public class BloomFilter_Tests
    {
        [NUnit.Framework.Test]
        public void BloomFilter_Smoke_Test()
        {
            var filter = new BloomFilter<string>(100);

            filter.AddKey("cat");
            filter.AddKey("rat");

            NUnit.Framework.Assert.IsTrue(filter.KeyExists("cat"));
            NUnit.Framework.Assert.IsFalse(filter.KeyExists("bat"));
        }

        [NUnit.Framework.Test]
        public void BloomFilter_Accuracy_Test()
        {
            var bloomFilter = new BloomFilter<string>(10000);

            bloomFilter.AddKey("foo");
            bloomFilter.AddKey("bar");
            bloomFilter.AddKey("apple");
            bloomFilter.AddKey("orange");
            bloomFilter.AddKey("banana");

            NUnit.Framework.Assert.IsTrue(bloomFilter.KeyExists("bar"));
            NUnit.Framework.Assert.IsFalse(bloomFilter.KeyExists("ba111r"));

            NUnit.Framework.Assert.IsTrue(bloomFilter.KeyExists("banana"));
            NUnit.Framework.Assert.IsFalse(bloomFilter.KeyExists("dfs11j"));

            NUnit.Framework.Assert.IsTrue(bloomFilter.KeyExists("foo"));
            NUnit.Framework.Assert.IsFalse(bloomFilter.KeyExists("1foo"));

            NUnit.Framework.Assert.IsTrue(bloomFilter.KeyExists("apple"));
            NUnit.Framework.Assert.IsFalse(bloomFilter.KeyExists("applefoo"));

            NUnit.Framework.Assert.IsTrue(bloomFilter.KeyExists("orange"));
            NUnit.Framework.Assert.IsFalse(bloomFilter.KeyExists("orangew"));
        }
    }
}
