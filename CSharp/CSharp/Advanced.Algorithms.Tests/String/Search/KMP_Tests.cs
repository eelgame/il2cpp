using Advanced.Algorithms.String;


namespace Advanced.Algorithms.Tests.String
{
    
    public class KMP_Tests
    {
        [HuaTuo.NUnit.Framework.Test]
        public void String_KMP_Test()
        {
            var algorithm = new KMP();

            var index = algorithm.Search("xabcabzabc", "abc");

            HuaTuo.NUnit.Framework.Assert.AreEqual(1, index);

            index = algorithm.Search("abdcdaabxaabxcaabxaabxay", "aabxaabxcaabxaabxay");

            HuaTuo.NUnit.Framework.Assert.AreEqual(5, index);

            index = algorithm.Search("aaaabaaaaaaa", "aaaa");

            HuaTuo.NUnit.Framework.Assert.AreEqual(0, index);

            index = algorithm.Search("abcabababdefgabcd", "fga");

            HuaTuo.NUnit.Framework.Assert.AreEqual(11, index);

            index = algorithm.Search("abxabcabcaby", "abcaby");

            HuaTuo.NUnit.Framework.Assert.AreEqual(6, index);

            index = algorithm.Search("abxabcabcaby", "abx");

            HuaTuo.NUnit.Framework.Assert.AreEqual(0, index);
        }
    }
}
