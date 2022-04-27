﻿using Advanced.Algorithms.Geometry;

using System.Collections.Generic;

namespace Advanced.Algorithms.Tests.Geometry
{

    
    public class ClosestPointPair_Tests
    {
        [HuaTuo.NUnit.Framework.Test]
        public void ClosestPointPair_Smoke_Test()
        {
            var testPoints = new List<int[]>()
            {
                new int[]{ 2, 3},
                new int[]{ 12, 30},
                new int[]{ 40, 50},
                new int[]{ 5, 1},
                new int[]{ 12, 10},
                new int[]{ 3, 4}
            };

            HuaTuo.NUnit.Framework.Assert.AreEqual(1.414, ClosestPointPair.Find(testPoints), 3);
        }
    }
}
