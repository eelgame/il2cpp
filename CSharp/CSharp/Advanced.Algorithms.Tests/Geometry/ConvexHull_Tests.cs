using Advanced.Algorithms.Geometry;

using System.Collections.Generic;

namespace Advanced.Algorithms.Tests.Geometry
{
    
    public class ConvexHull_Tests
    {
        [NUnit.Framework.Test]
        public void ConvexHull_Smoke_Test()
        {
            var testPoints = new List<int[]>()
            {
                new int[]{ 0, 3},
                new int[]{ 2, 2},
                new int[]{ 1, 1},
                new int[]{ 2, 1},
                new int[]{ 3, 0},
                new int[]{ 0, 0},
                new int[]{ 3, 3}
            };

            var result = ConvexHull.Find(testPoints);

            NUnit.Framework.Assert.AreEqual(4, result.Count);
        }
    }
}
