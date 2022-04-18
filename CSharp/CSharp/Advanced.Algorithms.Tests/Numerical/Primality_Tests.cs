using Advanced.Algorithms.Numerical;


namespace Advanced.Algorithms.Tests.Numerical
{
    
    public class Primality_Tests
    {
        [NUnit.Framework.Test]
        public void Prime_Smoke_Test()
        {
            NUnit.Framework.Assert.IsTrue(PrimeTester.IsPrime(11));
            NUnit.Framework.Assert.IsFalse(PrimeTester.IsPrime(50));
            NUnit.Framework.Assert.IsTrue(PrimeTester.IsPrime(101));
        }
    }
}
