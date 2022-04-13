using Algorithms.Numeric;
using System.Linq;
using NUnit.Framework;
using Fact = NUnit.Framework.TestAttribute;

namespace UnitTest.AlgorithmsTests
{
    public class SieveOfEratosthenesTests
    {
        private const int MaxNumber = 100;

        [Fact]
        public void SieveOfEratosthenesGeneratesCorrectResults()
        {
            var results = SieveOfEratosthenes.GeneratePrimesUpTo(MaxNumber);
            Assert.NotNull(results);
            Assert.True(results.Any());
           Assert.AreEqual(results.Count(), 25);
            Assert.Contains(2, results.ToArray());
            Assert.Contains(7, results.ToArray());
            Assert.Contains(23, results.ToArray());
            Assert.Contains(41, results.ToArray());
            Assert.Contains(97, results.ToArray());

        }

        [Fact]
        public void SieveOfEratosthenesReturnsEmptyListWhenGiven0()
        {
            var results = SieveOfEratosthenes.GeneratePrimesUpTo(0);
            Assert.NotNull(results);
            Assert.False(results.Any());
        }
    }
}