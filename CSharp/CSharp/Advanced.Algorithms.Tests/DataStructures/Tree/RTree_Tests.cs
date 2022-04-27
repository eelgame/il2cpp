﻿using Advanced.Algorithms.DataStructures;
using Advanced.Algorithms.Geometry;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Advanced.Algorithms.Tests.DataStructures
{
    
    public class RTree_Tests
    {
        /// </summary>
        [HuaTuo.NUnit.Framework.Test]
        public void RTree_Insertion_Test()
        {
            var nodeCount = 1000;
            var randomPolygons = new HashSet<Polygon>();
            for (int i = 0; i < nodeCount; i++)
            {
                randomPolygons.Add(getRandomPointOrPolygon());
            }
            var order = 5;
            var tree = new RTree(order);

            int j = 0;
            foreach (var polygon in randomPolygons)
            {
                tree.Insert(polygon);

                //IEnumerable test
                HuaTuo.NUnit.Framework.Assert.AreEqual(tree.Count, tree.Count());
                //height should be similar to that of B+ tree.
                //https://en.wikipedia.org/wiki/B-tree#Best_case_and_worst_case_heights
                var theoreticalMaxHeight = Math.Ceiling(Math.Log((j + 2) / 2, (int)Math.Ceiling((double)order / 2))) + 1;

                var actualMaxHeight = tree.Root.Height;
                HuaTuo.NUnit.Framework.Assert.AreEqual(verifyHeightUniformityAndReturnHeight(tree.Root, order), actualMaxHeight);
                HuaTuo.NUnit.Framework.Assert.IsTrue(actualMaxHeight <= theoreticalMaxHeight);
                j++;

                HuaTuo.NUnit.Framework.Assert.IsTrue(tree.Exists(polygon));
            }

            HuaTuo.NUnit.Framework.Assert.AreEqual(j, tree.Count);
        }

        /// </summary>
        [HuaTuo.NUnit.Framework.Test]
        public void RTree_Range_Search_Test()
        {
            var nodeCount = 1000;
            var randomPolygons = new HashSet<Polygon>();
            for (int i = 0; i < nodeCount; i++)
            {
                randomPolygons.Add(getRandomPointOrPolygon());
            }
            var order = 5;
            var tree = new RTree(order);

            foreach (var polygon in randomPolygons)
            {
                tree.Insert(polygon);
            }

            //IEnumerable test
            HuaTuo.NUnit.Framework.Assert.AreEqual(tree.Count, tree.Count());

            var searchRectangle = getRandomPointOrPolygon().GetContainingRectangle();

            var expectedIntersections = randomPolygons.Where(x => RectangleIntersection.FindIntersection(searchRectangle, x.GetContainingRectangle()) != null).ToList();
            var actualIntersections = tree.RangeSearch(searchRectangle);

            HuaTuo.NUnit.Framework.Assert.AreEqual(expectedIntersections.Count, actualIntersections.Count);
        }

        /// </summary>
        [HuaTuo.NUnit.Framework.Test]
        public void RTree_Deletion_Test()
        {
            var nodeCount = 1000;
            var randomPolygons = new HashSet<Polygon>();
            for (int i = 0; i < nodeCount; i++)
            {
                randomPolygons.Add(getRandomPointOrPolygon());
            }
            var order = 5;
            var tree = new RTree(order);

            foreach (var polygon in randomPolygons)
            {
                tree.Insert(polygon);
            }

            //IEnumerable test
            HuaTuo.NUnit.Framework.Assert.AreEqual(tree.Count, tree.Count());

            int j = randomPolygons.Count;
            foreach (var polygon in randomPolygons)
            {
                tree.Delete(polygon);
                HuaTuo.NUnit.Framework.Assert.IsFalse(tree.Exists(polygon));

                j--;

                if (j > 0)
                {
                    var actualMaxHeight = tree.Root.Height;
                    HuaTuo.NUnit.Framework.Assert.AreEqual(verifyHeightUniformityAndReturnHeight(tree.Root, order), actualMaxHeight);
                    HuaTuo.NUnit.Framework.Assert.AreEqual(j, tree.Count);
                }

            }
        }

        [HuaTuo.NUnit.Framework.Test]
        public void RTree_Stress_Test()
        {
            var nodeCount = 10000;
            var randomPolygons = new HashSet<Polygon>();
            for (int i = 0; i < nodeCount; i++)
            {
                randomPolygons.Add(getRandomPointOrPolygon());
            }

            var order = 5;
            var tree = new RTree(order);

            foreach (var polygon in randomPolygons)
            {
                tree.Insert(polygon);
            }

            //IEnumerable test
            HuaTuo.NUnit.Framework.Assert.AreEqual(tree.Count, tree.Count());

            foreach (var polygon in randomPolygons)
            {
                tree.Delete(polygon);
            }
        }

        /// <summary>
        ///     Verifies that all children have same height.
        /// </summary>
        /// <returns>Returns the height of given node.</returns>
        private int verifyHeightUniformityAndReturnHeight(RTreeNode node, int order)
        {
            if (!node.IsLeaf)
            {
                HuaTuo.NUnit.Framework.Assert.IsTrue(node.KeyCount >= order / 2);
            }

            var heights = new List<int>();
            foreach (var child in node.Children.Take(node.KeyCount))
            {
                heights.Add(verifyHeightUniformityAndReturnHeight(child, order) + 1);
            }

            if (node.KeyCount > 0)
            {
                var height = heights.Distinct();
                HuaTuo.NUnit.Framework.Assert.AreEqual(1, height.Count());
                return height.First();
            }

            return 0;
        }

        private static Random random = new Random();

        private static Polygon getRandomPointOrPolygon()
        {
            //if edge length is one we create a point otherwise we create a polygon
            var edgeLength = random.Next(1, 5);

            var edgePoints = new List<Point>();

            while (edgeLength > 0)
            {
                edgePoints.Add(new Point(random.Next(0, 100) * random.NextDouble(), random.Next(0, 100) * random.NextDouble()));
                edgeLength--;
            }

            return new Polygon(edgePoints);
        }
    }
}
