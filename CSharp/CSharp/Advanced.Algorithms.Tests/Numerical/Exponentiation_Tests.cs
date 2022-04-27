using Advanced.Algorithms.Numerical;


namespace Advanced.Algorithms.Tests.Numerical
{
    
    public class Exponentiation_Tests
    {
        [HuaTuo.NUnit.Framework.Test]
        public void Fast_Exponent_Smoke_Test()
        {
            var result = FastExponentiation.BySquaring(2, 5);

            HuaTuo.NUnit.Framework.Assert.AreEqual(32, result);

            result = FastExponentiation.BySquaring(2, 6);

            HuaTuo.NUnit.Framework.Assert.AreEqual(64, result);
        }
    }
}
