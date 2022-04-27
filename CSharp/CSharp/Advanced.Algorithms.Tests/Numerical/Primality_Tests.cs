using Advanced.Algorithms.Numerical;


namespace Advanced.Algorithms.Tests.Numerical
{
    
    public class Primality_Tests
    {
        [HuaTuo.NUnit.Framework.Test]
        public void Prime_Smoke_Test()
        {
            HuaTuo.NUnit.Framework.Assert.IsTrue(PrimeTester.IsPrime(11));
            HuaTuo.NUnit.Framework.Assert.IsFalse(PrimeTester.IsPrime(50));
            HuaTuo.NUnit.Framework.Assert.IsTrue(PrimeTester.IsPrime(101));
        }
    }
}
