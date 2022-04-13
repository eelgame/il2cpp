using Algorithms.Numeric;
using NUnit.Framework;
using Fact = NUnit.Framework.TestAttribute;

namespace UnitTest.AlgorithmsTests
{
    public class GreatestCommonDivisorTests
    {
        [Fact]
        public void FindGCD_BothAreZero()
        {
            var gcdEuclidean = GreatestCommonDivisor.FindGCDEuclidean(0, 0);
          Assert.AreEqual(0, gcdEuclidean);

            var gcdStein = GreatestCommonDivisor.FindGCDStein(0, 0);
          Assert.AreEqual(0, gcdStein);
        }

        [Theory]
        [TestCase(0, 4, 4)]
        [TestCase(0, 9, 9)]
        [TestCase(0, -14, 14)]
        [TestCase(0, -99, 99)]
        public void FindGCD_FirstIsZero(int a, int b, int expected)
        {
            var gcdEuclidean = GreatestCommonDivisor.FindGCDEuclidean(a, b);
          Assert.AreEqual(expected, gcdEuclidean);

            var gcdStein = GreatestCommonDivisor.FindGCDStein(a, b);
          Assert.AreEqual(expected, gcdStein);
        }

        [Theory]
        [TestCase(4, 0, 4)]
        [TestCase(9, 0, 9)]
        [TestCase(-14, 0, 14)]
        [TestCase(-99, 0, 99)]
        public void FindGCD_SecondIsZero(int a, int b, int expected)
        {
            var gcdEuclidean = GreatestCommonDivisor.FindGCDEuclidean(a, b);
          Assert.AreEqual(expected, gcdEuclidean);

            var gcdStein = GreatestCommonDivisor.FindGCDStein(a, b);
          Assert.AreEqual(expected, gcdStein);
        }

        [Theory]
        [TestCase(2, 4, 2)]
        [TestCase(27, 9, 9)]
        [TestCase(27, 14, 1)]
        [TestCase(9, 6, 3)]
        public void FindGCD_BothNumberArePositive(int a, int b, int expected)
        {
            var gcdEuclidean = GreatestCommonDivisor.FindGCDEuclidean(a, b);
          Assert.AreEqual(expected, gcdEuclidean);

            var gcdStein = GreatestCommonDivisor.FindGCDStein(a, b);
          Assert.AreEqual(expected, gcdStein);
        }

        [Theory]
        [TestCase(-2, -4, 2)]
        [TestCase(-27, -9, 9)]
        [TestCase(-27, -14, 1)]
        [TestCase(-9, -6, 3)]
        public void FindGCD_BothNumberAreNegative(int a, int b, int expected)
        {
            var gcdEuclidean = GreatestCommonDivisor.FindGCDEuclidean(a, b);
          Assert.AreEqual(expected, gcdEuclidean);

            var gcdStein = GreatestCommonDivisor.FindGCDStein(a, b);
          Assert.AreEqual(expected, gcdStein);
        }

        [Theory]
        [TestCase(2, -4, 2)]
        [TestCase(-27, 9, 9)]
        [TestCase(27, -14, 1)]
        [TestCase(-9, 6, 3)]
        public void FindGCD_CombinationPositiveAndNegative(int a, int b, int expected)
        {
            var gcdEuclidean = GreatestCommonDivisor.FindGCDEuclidean(a, b);
          Assert.AreEqual(expected, gcdEuclidean);

            var gcdStein = GreatestCommonDivisor.FindGCDStein(a, b);
          Assert.AreEqual(expected, gcdStein);
        }
    }
}
