using Advanced.Algorithms.String;


namespace Advanced.Algorithms.Tests.String
{
    
    public class Manacher_Tests
    {
        [HuaTuo.NUnit.Framework.Test]
        public void Manacher_Palindrome_Tests()
        {
            var manacher = new ManachersPalindrome();

            var length = manacher.FindLongestPalindrome("aacecaaa");
            HuaTuo.NUnit.Framework.Assert.IsTrue(length == 7);

            length = manacher.FindLongestPalindrome("baab");
            HuaTuo.NUnit.Framework.Assert.IsTrue(length == 4);

            length = manacher.FindLongestPalindrome("abaab");
            HuaTuo.NUnit.Framework.Assert.IsTrue(length == 4);

            length = manacher.FindLongestPalindrome("abaxabaxabb");
            HuaTuo.NUnit.Framework.Assert.IsTrue(length == 9);

            length = manacher.FindLongestPalindrome("abaxabaxabybaxabyb");
            HuaTuo.NUnit.Framework.Assert.IsTrue(length == 11);

            length = manacher.FindLongestPalindrome("abaxabaxabbaxabyb");
            HuaTuo.NUnit.Framework.Assert.IsTrue(length == 10);
        }
    }
}
