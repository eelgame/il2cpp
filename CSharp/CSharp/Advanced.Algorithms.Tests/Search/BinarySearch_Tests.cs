using Advanced.Algorithms.Search;


namespace Advanced.Algorithms.Tests.Search
{
    
    public class BinarySearch_Tests
    {
        [HuaTuo.NUnit.Framework.Test]
        public void Search_Smoke_Test()
        {
            var test = new int[] { 2, 3, 5, 7, 11, 13, 17, 19,
                23, 29, 31, 37, 41, 43, 47, 53, 59, 61, 67, 71, 73, 79 };

            HuaTuo.NUnit.Framework.Assert.AreEqual(15, BinarySearch.Search(test, 53));
            HuaTuo.NUnit.Framework.Assert.AreEqual(-1, BinarySearch.Search(test, 80));
        }
    }
}
