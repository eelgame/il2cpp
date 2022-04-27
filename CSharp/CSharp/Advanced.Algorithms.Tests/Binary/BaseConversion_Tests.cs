using Advanced.Algorithms.Binary;


namespace Advanced.Algorithms.Tests.Binary
{
    
    public class BaseConversion_Tests
    {
        [HuaTuo.NUnit.Framework.Test]
        public void BaseConversion_Smoke_Test()
        {
            //decimal to binary
            HuaTuo.NUnit.Framework.Assert.AreEqual("11",
              BaseConversion.Convert("1011", "01",
                  "0123456789"));

            //binary to decimal
            HuaTuo.NUnit.Framework.Assert.AreEqual("11.5",
                BaseConversion.Convert("1011.10", "01",
               "0123456789"));

            //decimal to base3
            HuaTuo.NUnit.Framework.Assert.AreEqual("Foo",
                BaseConversion.Convert("9", "0123456789",
                    "oF8"));

            //base3 to decimal 
            HuaTuo.NUnit.Framework.Assert.AreEqual("9",
                BaseConversion.Convert("Foo", "oF8",
                    "0123456789"));

            //hex to binary
            HuaTuo.NUnit.Framework.Assert.AreEqual("10011",
                BaseConversion.Convert("13", "0123456789abcdef",
                    "01"));

            //decimal to hex
            HuaTuo.NUnit.Framework.Assert.AreEqual("5.0e631f8a0902de00d1b71758e219652b",
                BaseConversion.Convert("5.05620", "0123456789",
                    "0123456789abcdef"));

            //hex to decimal with precision 5
            HuaTuo.NUnit.Framework.Assert.AreEqual("5.05619",
               BaseConversion.Convert("5.0e631f8a0902de00d1b71758e219652b", "0123456789abcdef",
                    "0123456789", 5));

        }
    }
}
