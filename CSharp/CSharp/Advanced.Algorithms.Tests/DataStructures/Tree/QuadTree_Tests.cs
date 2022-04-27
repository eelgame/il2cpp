﻿using Advanced.Algorithms.DataStructures;
using Advanced.Algorithms.Geometry;

using System.Linq;

namespace Advanced.Algorithms.Tests.DataStructures
{
    public class QuadTree_Tests
    {
        
        public class QuadTreeTests
        {
            [HuaTuo.NUnit.Framework.Test]
            public void QuadTree_Smoke_Test()
            {
                var tree = new QuadTree<object>();

                tree.Insert(new Point(0, 1));
                tree.Insert(new Point(1, 1));
                tree.Insert(new Point(2, 5));
                tree.Insert(new Point(3, 6));
                tree.Insert(new Point(4, 5));
                tree.Insert(new Point(4, 7));
                tree.Insert(new Point(5, 8));
                tree.Insert(new Point(6, 9));
                tree.Insert(new Point(7, 10));

                var rangeResult = tree.RangeSearch(new Rectangle(new Point(1, 7), new Point(3, 1)));
                HuaTuo.NUnit.Framework.Assert.IsTrue(rangeResult.Count == 3);

                //IEnumerable test using linq
                HuaTuo.NUnit.Framework.Assert.AreEqual(tree.Count, tree.Count());

                tree.Delete(new Point(2, 5));
                rangeResult = tree.RangeSearch(new Rectangle(new Point(1, 7), new Point(3, 1)));
                HuaTuo.NUnit.Framework.Assert.IsTrue(rangeResult.Count == 2);

                tree.Delete(new Point(3, 6));
                rangeResult = tree.RangeSearch(new Rectangle(new Point(1, 7), new Point(3, 1)));
                HuaTuo.NUnit.Framework.Assert.IsTrue(rangeResult.Count == 1);

                tree.Delete(new Point(0, 1));
                tree.Delete(new Point(1, 1));
                tree.Delete(new Point(4, 5));
                tree.Delete(new Point(4, 7));
                tree.Delete(new Point(5, 8));
                tree.Delete(new Point(6, 9));
                tree.Delete(new Point(7, 10));
            }
        }
    }
}
