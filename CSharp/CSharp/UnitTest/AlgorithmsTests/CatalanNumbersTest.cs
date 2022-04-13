using System.Collections.Generic;
using System.Numerics;
using Algorithms.Numeric;
using NUnit.Framework;
using Fact = NUnit.Framework.TestAttribute;

namespace UnitTest.AlgorithmsTests
{
    public class CatalanNumbersTest
    {
        [Fact]
        public void DoTest()
        {
            var list = CatalanNumbers.GetRange(0, 100);
            var list2 = new List<BigInteger>();

            // TRY CALCULATING FROM Bin.Coeff.
            for (uint i = 0; i < list.Count; ++i)
            {
                var catalanNumber = CatalanNumbers.GetNumberByBinomialCoefficients(i);
                list2.Add(catalanNumber);

                Assert.True(list[(int)i] == list2[(int)i], "Wrong calculation.");
            }
        }

        // Values retrieved from https://oeis.org/A000108/list.
        [Theory]
        [TestCase(0, "1")]
        [TestCase(1, "1")]
        [TestCase(2, "2")]
        [TestCase(3, "5")]
        [TestCase(4, "14")]
        [TestCase(5, "42")]
        [TestCase(6, "132")]
        [TestCase(7, "429")]
        [TestCase(8, "1430")]
        [TestCase(9, "4862")]
        [TestCase(10, "16796")]
        [TestCase(11, "58786")]
        [TestCase(12, "208012")]
        [TestCase(13, "742900")]
        [TestCase(14, "2674440")]
        [TestCase(15, "9694845")]
        [TestCase(16, "35357670")]
        [TestCase(17, "129644790")]
        [TestCase(18, "477638700")]
        [TestCase(19, "1767263190")]
        [TestCase(20, "6564120420")]
        [TestCase(21, "24466267020")]
        [TestCase(22, "91482563640")]
        [TestCase(23, "343059613650")]
        [TestCase(24, "1289904147324")]
        [TestCase(25, "4861946401452")]
        [TestCase(26, "18367353072152")]
        [TestCase(27, "69533550916004")]
        [TestCase(28, "263747951750360")]
        [TestCase(29, "1002242216651368")]
        [TestCase(30, "3814986502092304")]
        public void ManuallyVerifyCatalanNumber(long rank, string value)
        {
            // This conversion seems to be necessary because as of this
            // writing xunit doesn't behave well with BigInteger inline
            // data values.
            var bigint = BigInteger.Parse(value);

            Assert.True(CatalanNumbers.GetNumber((uint) rank) == bigint);
        }
    }
}