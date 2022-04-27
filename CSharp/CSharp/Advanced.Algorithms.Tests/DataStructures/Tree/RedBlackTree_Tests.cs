using Advanced.Algorithms.DataStructures;

using System;
using System.Linq;

namespace Advanced.Algorithms.Tests.DataStructures
{
    
    public class RedBlackTree_Tests
    {
        /// <summary>
        ///  Smoke test
        /// </summary>
        [HuaTuo.NUnit.Framework.Test]
        public void RedBlackTree_Smoke_Test()
        {
            //insert test
            var tree = new RedBlackTree<int>();

            HuaTuo.NUnit.Framework.Assert.AreEqual(-1, tree.Root.GetHeight());

            tree.Insert(1);
            tree.Insert(2);
            tree.Insert(3);
            tree.Insert(4);
            tree.Insert(5);
            tree.Insert(6);
            tree.Insert(7);
            tree.Insert(8);
            tree.Insert(9);
            tree.Insert(10);
            tree.Insert(11);

            HuaTuo.NUnit.Framework.Assert.AreEqual(11, tree.Count);

            //IEnumerable test using linq
            HuaTuo.NUnit.Framework.Assert.AreEqual(tree.Count, tree.Count());
            HuaTuo.NUnit.Framework.Assert.AreEqual(tree.Count, tree.AsEnumerableDesc().Count());

            //delete
            tree.Delete(1);
            tree.Delete(2);
            tree.Delete(3);
            tree.Delete(4);
            tree.Delete(5);
            tree.Delete(6);
            tree.Delete(7);
            tree.Delete(8);
            tree.Delete(9);
            tree.Delete(10);
            tree.Delete(11);

            HuaTuo.NUnit.Framework.Assert.AreEqual(0, tree.Count);
        }

        [HuaTuo.NUnit.Framework.Test]
        public void RedBlackTree_Accuracy_Test()
        {
            var nodeCount = 1000;

            var rnd = new Random();
            var sorted = Enumerable.Range(1, nodeCount).ToList();
            var randomNumbers = sorted
                                .OrderBy(x => rnd.Next())
                                .ToList();

            var tree = new RedBlackTree<int>();

            for (int i = 0; i < nodeCount; i++)
            {
                var index = tree.Insert(randomNumbers[i]);
                HuaTuo.NUnit.Framework.Assert.AreEqual(index, tree.IndexOf(randomNumbers[i]));
                HuaTuo.NUnit.Framework.Assert.IsTrue(tree.HasItem(randomNumbers[i]));
                HuaTuo.NUnit.Framework.Assert.IsTrue(tree.Root.IsBinarySearchTree(int.MinValue, int.MaxValue));
                tree.Root.VerifyCount();
                var actualHeight = tree.Root.GetHeight();

                //http://doctrina.org/maximum-height-of-red-black-tree.html
                var maxHeight = 2 * Math.Log(nodeCount + 1, 2);

                HuaTuo.NUnit.Framework.Assert.IsTrue(actualHeight < maxHeight);
                HuaTuo.NUnit.Framework.Assert.IsTrue(tree.Count == i + 1);
            }

            for (int i = 0; i < sorted.Count; i++)
            {
                HuaTuo.NUnit.Framework.Assert.AreEqual(sorted[i], tree.ElementAt(i));
                HuaTuo.NUnit.Framework.Assert.AreEqual(i, tree.IndexOf(sorted[i]));
            }

            //shuffle again before deletion tests
            randomNumbers = Enumerable.Range(1, nodeCount)
                                   .OrderBy(x => rnd.Next())
                                   .ToList();

            //IEnumerable test using linq
            HuaTuo.NUnit.Framework.Assert.AreEqual(tree.Count, tree.Count());
            HuaTuo.NUnit.Framework.Assert.AreEqual(tree.Count, tree.AsEnumerableDesc().Count());

            for (int i = 0; i < nodeCount; i++)
            {
                if (rnd.NextDouble() >= 0.5)
                {
                    var index = tree.IndexOf(randomNumbers[i]);
                    HuaTuo.NUnit.Framework.Assert.AreEqual(index, tree.Delete(randomNumbers[i]));
                }
                else
                {
                    var index = tree.IndexOf(randomNumbers[i]);
                    HuaTuo.NUnit.Framework.Assert.AreEqual(tree.ElementAt(index), randomNumbers[i]);
                    tree.RemoveAt(index);
                }

                HuaTuo.NUnit.Framework.Assert.IsTrue(tree.Root.IsBinarySearchTree(int.MinValue, int.MaxValue));
                tree.Root.VerifyCount();
                var actualHeight = tree.Root.GetHeight();

                //http://doctrina.org/maximum-height-of-red-black-tree.html
                var maxHeight = 2 * Math.Log(nodeCount + 1, 2);

                HuaTuo.NUnit.Framework.Assert.IsTrue(actualHeight < maxHeight);
                HuaTuo.NUnit.Framework.Assert.IsTrue(tree.Count == nodeCount - 1 - i);
            }

            HuaTuo.NUnit.Framework.Assert.IsTrue(tree.Count == 0);
        }

        [HuaTuo.NUnit.Framework.Test]
        public void RedBlack_Accuracy_Test_With_Node_LookUp()
        {
            var nodeCount = 1000;

            var rnd = new Random();
            var sorted = Enumerable.Range(1, nodeCount).ToList();
            var randomNumbers = sorted
                                .OrderBy(x => rnd.Next())
                                .ToList();

            var tree = new RedBlackTree<int>(true);

            for (int i = 0; i < nodeCount; i++)
            {
                var index = tree.Insert(randomNumbers[i]);
                HuaTuo.NUnit.Framework.Assert.AreEqual(index, tree.IndexOf(randomNumbers[i]));
                HuaTuo.NUnit.Framework.Assert.IsTrue(tree.HasItem(randomNumbers[i]));
                HuaTuo.NUnit.Framework.Assert.IsTrue(tree.Root.IsBinarySearchTree(int.MinValue, int.MaxValue));
                tree.Root.VerifyCount();
                var actualHeight = tree.Root.GetHeight();

                //http://doctrina.org/maximum-height-of-red-black-tree.html
                var maxHeight = 2 * Math.Log(nodeCount + 1, 2);

                HuaTuo.NUnit.Framework.Assert.IsTrue(actualHeight < maxHeight);
                HuaTuo.NUnit.Framework.Assert.IsTrue(tree.Count == i + 1);
            }

            for (int i = 0; i < sorted.Count; i++)
            {
                HuaTuo.NUnit.Framework.Assert.AreEqual(sorted[i], tree.ElementAt(i));
                HuaTuo.NUnit.Framework.Assert.AreEqual(i, tree.IndexOf(sorted[i]));
            }

            //shuffle again before deletion tests
            randomNumbers = Enumerable.Range(1, nodeCount)
                                   .OrderBy(x => rnd.Next())
                                   .ToList();

            //IEnumerable test using linq
            HuaTuo.NUnit.Framework.Assert.AreEqual(tree.Count, tree.Count());
            HuaTuo.NUnit.Framework.Assert.AreEqual(tree.Count, tree.AsEnumerableDesc().Count());

            for (int i = 0; i < nodeCount; i++)
            {
                if (rnd.NextDouble() >= 0.5)
                {
                    var index = tree.IndexOf(randomNumbers[i]);
                    HuaTuo.NUnit.Framework.Assert.AreEqual(index, tree.Delete(randomNumbers[i]));
                }
                else
                {
                    var index = tree.IndexOf(randomNumbers[i]);
                    HuaTuo.NUnit.Framework.Assert.AreEqual(tree.ElementAt(index), randomNumbers[i]);
                    tree.RemoveAt(index);
                }

                HuaTuo.NUnit.Framework.Assert.IsTrue(tree.Root.IsBinarySearchTree(int.MinValue, int.MaxValue));
                tree.Root.VerifyCount();
                var actualHeight = tree.Root.GetHeight();

                //http://doctrina.org/maximum-height-of-red-black-tree.html
                var maxHeight = 2 * Math.Log(nodeCount + 1, 2);

                HuaTuo.NUnit.Framework.Assert.IsTrue(actualHeight < maxHeight);
                HuaTuo.NUnit.Framework.Assert.IsTrue(tree.Count == nodeCount - 1 - i);
            }

            HuaTuo.NUnit.Framework.Assert.IsTrue(tree.Count == 0);
        }

        [HuaTuo.NUnit.Framework.Test]
        public void RedBlackTree_BulkInit_Test()
        {
            var nodeCount = 1000;

            var rnd = new Random();
            var sortedNumbers = Enumerable.Range(1, nodeCount).ToList();

            var tree = new RedBlackTree<int>(sortedNumbers);

            HuaTuo.NUnit.Framework.Assert.IsTrue(tree.Root.IsBinarySearchTree(int.MinValue, int.MaxValue));

            //IEnumerable test using linq
            HuaTuo.NUnit.Framework.Assert.AreEqual(tree.Count, tree.Count());
            HuaTuo.NUnit.Framework.Assert.AreEqual(tree.Count, tree.AsEnumerableDesc().Count());

            tree.Root.VerifyCount();

            for (int i = 0; i < nodeCount; i++)
            {
                tree.Delete(sortedNumbers[i]);

                tree.Root.VerifyCount();
                HuaTuo.NUnit.Framework.Assert.IsTrue(tree.Root.IsBinarySearchTree(int.MinValue, int.MaxValue));

                var actualHeight = tree.Root.GetHeight();

                //http://doctrina.org/maximum-height-of-red-black-tree.html
                var maxHeight = 2 * Math.Log(nodeCount + 1, 2);

                HuaTuo.NUnit.Framework.Assert.IsTrue(actualHeight < maxHeight);
                HuaTuo.NUnit.Framework.Assert.IsTrue(tree.Count == nodeCount - 1 - i);
            }

            HuaTuo.NUnit.Framework.Assert.IsTrue(tree.Count == 0);
        }


        [HuaTuo.NUnit.Framework.Test]
        public void RedBlackTree_BulkInit_Test_With_Node_LookUp()
        {
            var nodeCount = 1000;

            var rnd = new Random();
            var sortedNumbers = Enumerable.Range(1, nodeCount).ToList();

            var tree = new RedBlackTree<int>(sortedNumbers, true);

            HuaTuo.NUnit.Framework.Assert.IsTrue(tree.Root.IsBinarySearchTree(int.MinValue, int.MaxValue));

            //IEnumerable test using linq
            HuaTuo.NUnit.Framework.Assert.AreEqual(tree.Count, tree.Count());
            HuaTuo.NUnit.Framework.Assert.AreEqual(tree.Count, tree.AsEnumerableDesc().Count());

            tree.Root.VerifyCount();

            for (int i = 0; i < nodeCount; i++)
            {
                tree.Delete(sortedNumbers[i]);

                tree.Root.VerifyCount();
                HuaTuo.NUnit.Framework.Assert.IsTrue(tree.Root.IsBinarySearchTree(int.MinValue, int.MaxValue));

                var actualHeight = tree.Root.GetHeight();

                //http://doctrina.org/maximum-height-of-red-black-tree.html
                var maxHeight = 2 * Math.Log(nodeCount + 1, 2);

                HuaTuo.NUnit.Framework.Assert.IsTrue(actualHeight < maxHeight);
                HuaTuo.NUnit.Framework.Assert.IsTrue(tree.Count == nodeCount - 1 - i);
            }

            HuaTuo.NUnit.Framework.Assert.IsTrue(tree.Count == 0);
        }

        [HuaTuo.NUnit.Framework.Test]
        public void RedBlackTree_StressTest()
        {
            var nodeCount = 1000 * 10;

            var rnd = new Random();
            var randomNumbers = Enumerable.Range(1, nodeCount)
                                .OrderBy(x => rnd.Next())
                                .ToList();

            var tree = new RedBlackTree<int>();

            for (int i = 0; i < nodeCount; i++)
            {
                tree.Insert(randomNumbers[i]);
                HuaTuo.NUnit.Framework.Assert.IsTrue(tree.Count == i + 1);
            }


            //shuffle again before deletion tests
            randomNumbers = Enumerable.Range(1, nodeCount)
                                   .OrderBy(x => rnd.Next())
                                   .ToList();

            //IEnumerable test using linq
            HuaTuo.NUnit.Framework.Assert.AreEqual(tree.Count, tree.Count());
            HuaTuo.NUnit.Framework.Assert.AreEqual(tree.Count, tree.AsEnumerableDesc().Count());

            for (int i = 0; i < nodeCount; i++)
            {
                tree.Delete(randomNumbers[i]);
                HuaTuo.NUnit.Framework.Assert.IsTrue(tree.Count == nodeCount - 1 - i);
            }

            HuaTuo.NUnit.Framework.Assert.IsTrue(tree.Count == 0);
        }
    }
}
