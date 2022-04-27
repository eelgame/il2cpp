using Advanced.Algorithms.Binary;



namespace Advanced.Algorithms.Tests.Binary
{
    
    public class Logarithm_Tests
    {
        [HuaTuo.NUnit.Framework.Test]
        public void Logarithm_Smoke_Test()
        {
            HuaTuo.NUnit.Framework.Assert.AreEqual(3, Logarithm.CalcBase2LogFloor(9));
            HuaTuo.NUnit.Framework.Assert.AreEqual(3, Logarithm.CalcBase2LogFloor(8));
            HuaTuo.NUnit.Framework.Assert.AreEqual(5, Logarithm.CalcBase2LogFloor(32));

            HuaTuo.NUnit.Framework.Assert.AreEqual(2, Logarithm.CalcBase10LogFloor(102));
            HuaTuo.NUnit.Framework.Assert.AreEqual(3, Logarithm.CalcBase10LogFloor(1000));
        }
    }
}
