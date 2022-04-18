using Advanced.Algorithms.String;


namespace Advanced.Algorithms.Tests.String
{
    
    public class Manacher_Tests
    {
        [NUnit.Framework.Test]
        public void Manacher_Palindrome_Tests()
        {
            var manacher = new ManachersPalindrome();

            var length = manacher.FindLongestPalindrome("aacecaaa");
            NUnit.Framework.Assert.IsTrue(length == 7);

            length = manacher.FindLongestPalindrome("baab");
            NUnit.Framework.Assert.IsTrue(length == 4);

            length = manacher.FindLongestPalindrome("abaab");
            NUnit.Framework.Assert.IsTrue(length == 4);

            length = manacher.FindLongestPalindrome("abaxabaxabb");
            NUnit.Framework.Assert.IsTrue(length == 9);

            length = manacher.FindLongestPalindrome("abaxabaxabybaxabyb");
            NUnit.Framework.Assert.IsTrue(length == 11);

            length = manacher.FindLongestPalindrome("abaxabaxabbaxabyb");
            NUnit.Framework.Assert.IsTrue(length == 10);
        }
    }
}
