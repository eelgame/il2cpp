using System;
using DataStructures.Common;
using NUnit.Framework;
using Fact = NUnit.Framework.TestAttribute;

namespace UnitTest.DataStructuresTests
{
    public class PrimeListTest
    {
        // [Fact]
        public void DoTest()
        {
            var instance = PrimesList.Instance;
          Assert.AreEqual(10000, instance.Count);
        }

        // [Fact]
        public void PrimesListIsReadOnly()
        {
            var instance = PrimesList.Instance;
            NotSupportedException ex = Assert.Throws<NotSupportedException>(() => instance.GetAll[0] = -1);
          Assert.AreEqual("Collection is read-only.", ex.Message);
        }
    }
}
