using Advanced.Algorithms.Numerical;


namespace Advanced.Algorithms.Tests.Numerical
{
    
    public class PrimeGenerator_Tests
    {
        [NUnit.Framework.Test]
        public void Prime_Generation_Smoke_Test()
        {
            NUnit.Framework.Assert.AreEqual(5, PrimeGenerator.GetAllPrimes(11).Count);
            NUnit.Framework.Assert.AreEqual(8, PrimeGenerator.GetAllPrimes(20).Count);
        }
    }
}
