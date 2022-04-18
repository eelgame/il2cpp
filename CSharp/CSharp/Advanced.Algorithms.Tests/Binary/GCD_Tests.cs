using Advanced.Algorithms.Binary;


namespace Advanced.Algorithms.Tests.Binary
{
    
    public class GCD_Tests
    {
        [NUnit.Framework.Test]
        public void GCD_Smoke_Test()
        {
            NUnit.Framework.Assert.AreEqual(3, Gcd.Find(-9, 3));
            NUnit.Framework.Assert.AreEqual(15, Gcd.Find(45, 30));

            NUnit.Framework.Assert.AreEqual(1, Gcd.Find(3, 5));

        }
    }
}
