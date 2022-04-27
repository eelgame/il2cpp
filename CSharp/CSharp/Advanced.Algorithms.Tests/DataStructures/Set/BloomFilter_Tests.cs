using Advanced.Algorithms.DataStructures;



namespace Advanced.Algorithms.Tests.DataStructures
{
    
    public class BloomFilter_Tests
    {
        [HuaTuo.NUnit.Framework.Test]
        public void BloomFilter_Smoke_Test()
        {
            var filter = new BloomFilter<string>(100);

            filter.AddKey("cat");
            filter.AddKey("rat");

            HuaTuo.NUnit.Framework.Assert.IsTrue(filter.KeyExists("cat"));
            HuaTuo.NUnit.Framework.Assert.IsFalse(filter.KeyExists("bat"));
        }

        [HuaTuo.NUnit.Framework.Test]
        public void BloomFilter_Accuracy_Test()
        {
            var bloomFilter = new BloomFilter<string>(10000);

            bloomFilter.AddKey("foo");
            bloomFilter.AddKey("bar");
            bloomFilter.AddKey("apple");
            bloomFilter.AddKey("orange");
            bloomFilter.AddKey("banana");

            HuaTuo.NUnit.Framework.Assert.IsTrue(bloomFilter.KeyExists("bar"));
            HuaTuo.NUnit.Framework.Assert.IsFalse(bloomFilter.KeyExists("ba111r"));

            HuaTuo.NUnit.Framework.Assert.IsTrue(bloomFilter.KeyExists("banana"));
            HuaTuo.NUnit.Framework.Assert.IsFalse(bloomFilter.KeyExists("dfs11j"));

            HuaTuo.NUnit.Framework.Assert.IsTrue(bloomFilter.KeyExists("foo"));
            HuaTuo.NUnit.Framework.Assert.IsFalse(bloomFilter.KeyExists("1foo"));

            HuaTuo.NUnit.Framework.Assert.IsTrue(bloomFilter.KeyExists("apple"));
            HuaTuo.NUnit.Framework.Assert.IsFalse(bloomFilter.KeyExists("applefoo"));

            HuaTuo.NUnit.Framework.Assert.IsTrue(bloomFilter.KeyExists("orange"));
            HuaTuo.NUnit.Framework.Assert.IsFalse(bloomFilter.KeyExists("orangew"));
        }
    }
}
