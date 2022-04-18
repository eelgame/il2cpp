using Advanced.Algorithms.String;



namespace Advanced.Algorithms.Tests.String
{
    
    public class RabinKarp_Tests
    {
        [NUnit.Framework.Test]
        public void String_RabinKarp_Test()
        {
            var algorithm = new RabinKarp();

            var index = algorithm.Search("xabcabzabc", "abc");

            NUnit.Framework.Assert.AreEqual(1, index);

            index = algorithm.Search("abdcdaabxaabxcaabxaabxay", "aabxaabxcaabxaabxay");

            NUnit.Framework.Assert.AreEqual(5, index);

            index = algorithm.Search("aaaabaaaaaaa", "aaaa");

            NUnit.Framework.Assert.AreEqual(0, index);

            index = algorithm.Search("abcabababdefgabcd", "fga");

            NUnit.Framework.Assert.AreEqual(11, index);

            index = algorithm.Search("abxabcabcaby", "abcaby");

            NUnit.Framework.Assert.AreEqual(6, index);

            index = algorithm.Search("abxabcabcaby", "abx");

            NUnit.Framework.Assert.AreEqual(0, index);
        }
    }
}
