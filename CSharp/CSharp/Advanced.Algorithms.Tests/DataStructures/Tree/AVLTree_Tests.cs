using Advanced.Algorithms.DataStructures;

using System;
using System.Linq;

namespace Advanced.Algorithms.Tests.DataStructures
{
    
    public class AvlTreeTests
    {
        /// <summary>
        /// Smoke test
        /// </summary>
        [HuaTuo.NUnit.Framework.Test]
        public void AVLTree_Smoke_Test()
        {
            //insert test
            var tree = new AVLTree<int>();
            HuaTuo.NUnit.Framework.Assert.AreEqual(-1, tree.GetHeight());

            tree.Insert(1);
            HuaTuo.NUnit.Framework.Assert.AreEqual(0, tree.GetHeight());

            tree.Insert(2);
            HuaTuo.NUnit.Framework.Assert.AreEqual(1, tree.GetHeight());

            tree.Insert(3);
            HuaTuo.NUnit.Framework.Assert.AreEqual(1, tree.GetHeight());

            tree.Insert(4);
            HuaTuo.NUnit.Framework.Assert.AreEqual(2, tree.GetHeight());

            tree.Insert(5);
            HuaTuo.NUnit.Framework.Assert.AreEqual(2, tree.GetHeight());

            tree.Insert(6);
            HuaTuo.NUnit.Framework.Assert.AreEqual(2, tree.GetHeight());

            tree.Insert(7);
            HuaTuo.NUnit.Framework.Assert.AreEqual(2, tree.GetHeight());

            tree.Insert(8);
            HuaTuo.NUnit.Framework.Assert.AreEqual(3, tree.GetHeight());

            tree.Insert(9);
            HuaTuo.NUnit.Framework.Assert.AreEqual(3, tree.GetHeight());

            tree.Insert(10);
            HuaTuo.NUnit.Framework.Assert.AreEqual(3, tree.GetHeight());

            tree.Insert(11);
            HuaTuo.NUnit.Framework.Assert.AreEqual(3, tree.GetHeight());

            //IEnumerable test using linq
            HuaTuo.NUnit.Framework.Assert.AreEqual(tree.Count, tree.Count());

            //delete
            tree.Delete(1);
            HuaTuo.NUnit.Framework.Assert.AreEqual(3, tree.GetHeight());

            tree.Delete(2);
            HuaTuo.NUnit.Framework.Assert.AreEqual(3, tree.GetHeight());

            tree.Delete(3);
            HuaTuo.NUnit.Framework.Assert.AreEqual(3, tree.GetHeight());

            tree.Delete(4);
            HuaTuo.NUnit.Framework.Assert.AreEqual(2, tree.GetHeight());

            tree.Delete(5);
            HuaTuo.NUnit.Framework.Assert.AreEqual(2, tree.GetHeight());

            tree.Delete(6);
            HuaTuo.NUnit.Framework.Assert.AreEqual(2, tree.GetHeight());

            tree.Delete(7);
            HuaTuo.NUnit.Framework.Assert.AreEqual(2, tree.GetHeight());

            tree.Delete(8);
            HuaTuo.NUnit.Framework.Assert.AreEqual(1, tree.GetHeight());

            tree.Delete(9);
            HuaTuo.NUnit.Framework.Assert.AreEqual(1, tree.GetHeight());

            tree.Delete(10);
            HuaTuo.NUnit.Framework.Assert.AreEqual(0, tree.GetHeight());

            tree.Delete(11);
            HuaTuo.NUnit.Framework.Assert.AreEqual(tree.GetHeight(), -1);

            HuaTuo.NUnit.Framework.Assert.AreEqual(tree.Count, 0);

            tree.Insert(31);
        }

        [HuaTuo.NUnit.Framework.Test]
        public void AVLTree_Accuracy_Test()
        {
            var nodeCount = 1000;

            var rnd = new Random();
            var sorted = Enumerable.Range(1, nodeCount).ToList();
            var randomNumbers = sorted
                                .OrderBy(x => rnd.Next())
                                .ToList();

            var tree = new AVLTree<int>();

            for (int i = 0; i < nodeCount; i++)
            {
                tree.Insert(randomNumbers[i]);

                HuaTuo.NUnit.Framework.Assert.IsTrue(tree.HasItem(randomNumbers[i]));
                HuaTuo.NUnit.Framework.Assert.IsTrue(tree.Root.IsBinarySearchTree(int.MinValue, int.MaxValue));
                tree.Root.VerifyCount();

                var actualHeight = tree.GetHeight();

                //http://stackoverflow.com/questions/30769383/finding-the-minimum-and-maximum-height-in-a-avl-tree-given-a-number-of-nodes
                var maxHeight = 1.44 * Math.Log(nodeCount + 2, 2) - 0.328;

                HuaTuo.NUnit.Framework.Assert.IsTrue(actualHeight < maxHeight);
                HuaTuo.NUnit.Framework.Assert.IsTrue(tree.Count == i + 1);
            }

            for (int i = 0; i < sorted.Count; i++)
            {
                HuaTuo.NUnit.Framework.Assert.AreEqual(sorted[i], tree.ElementAt(i));
                HuaTuo.NUnit.Framework.Assert.AreEqual(i, tree.IndexOf(sorted[i]));
            }

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
                    tree.Delete(randomNumbers[i]);
                }
                else
                {
                    var index = tree.IndexOf(randomNumbers[i]);
                    HuaTuo.NUnit.Framework.Assert.AreEqual(tree.ElementAt(index), randomNumbers[i]);
                    tree.RemoveAt(index);
                }

                HuaTuo.NUnit.Framework.Assert.IsTrue(tree.Root.IsBinarySearchTree(int.MinValue, int.MaxValue));
                tree.Root.VerifyCount();

                var actualHeight = tree.GetHeight();

                //http://stackoverflow.com/questions/30769383/finding-the-minimum-and-maximum-height-in-a-avl-tree-given-a-number-of-nodes
                var maxHeight = 1.44 * Math.Log(nodeCount + 2, 2) - 0.328;

                HuaTuo.NUnit.Framework.Assert.IsTrue(actualHeight < maxHeight);
            }

            HuaTuo.NUnit.Framework.Assert.IsTrue(tree.Count == 0);
        }

        [HuaTuo.NUnit.Framework.Test]
        public void AVLTree_Accuracy_Test_With_Node_LookUp()
        {
            var nodeCount = 1000;

            var rnd = new Random();
            var sorted = Enumerable.Range(1, nodeCount).ToList();
            var randomNumbers = sorted
                                .OrderBy(x => rnd.Next())
                                .ToList();

            var tree = new AVLTree<int>(true);

            for (int i = 0; i < nodeCount; i++)
            {
                tree.Insert(randomNumbers[i]);

                HuaTuo.NUnit.Framework.Assert.IsTrue(tree.HasItem(randomNumbers[i]));
                HuaTuo.NUnit.Framework.Assert.IsTrue(tree.Root.IsBinarySearchTree(int.MinValue, int.MaxValue));
                tree.Root.VerifyCount();

                var actualHeight = tree.GetHeight();

                //http://stackoverflow.com/questions/30769383/finding-the-minimum-and-maximum-height-in-a-avl-tree-given-a-number-of-nodes
                var maxHeight = 1.44 * Math.Log(nodeCount + 2, 2) - 0.328;

                HuaTuo.NUnit.Framework.Assert.IsTrue(actualHeight < maxHeight);
                HuaTuo.NUnit.Framework.Assert.IsTrue(tree.Count == i + 1);
            }

            for (int i = 0; i < sorted.Count; i++)
            {
                HuaTuo.NUnit.Framework.Assert.AreEqual(sorted[i], tree.ElementAt(i));
                HuaTuo.NUnit.Framework.Assert.AreEqual(i, tree.IndexOf(sorted[i]));
            }

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
                    tree.Delete(randomNumbers[i]);
                }
                else
                {
                    var index = tree.IndexOf(randomNumbers[i]);
                    HuaTuo.NUnit.Framework.Assert.AreEqual(tree.ElementAt(index), randomNumbers[i]);
                    tree.RemoveAt(index);
                }

                HuaTuo.NUnit.Framework.Assert.IsTrue(tree.Root.IsBinarySearchTree(int.MinValue, int.MaxValue));
                tree.Root.VerifyCount();

                var actualHeight = tree.GetHeight();

                //http://stackoverflow.com/questions/30769383/finding-the-minimum-and-maximum-height-in-a-avl-tree-given-a-number-of-nodes
                var maxHeight = 1.44 * Math.Log(nodeCount + 2, 2) - 0.328;

                HuaTuo.NUnit.Framework.Assert.IsTrue(actualHeight < maxHeight);
            }

            HuaTuo.NUnit.Framework.Assert.IsTrue(tree.Count == 0);
        }

        [HuaTuo.NUnit.Framework.Test]
        public void AVLTree_BulkInit_Test_With_Node_LookUp()
        {
            var nodeCount = 1000;

            var rnd = new Random();
            var randomNumbers = Enumerable.Range(1, nodeCount).ToList();

            var tree = new AVLTree<int>(randomNumbers);

            HuaTuo.NUnit.Framework.Assert.IsTrue(tree.Root.IsBinarySearchTree(int.MinValue, int.MaxValue));
            HuaTuo.NUnit.Framework.Assert.AreEqual(tree.Count, tree.Count());

            tree.Root.VerifyCount();

            for (int i = 0; i < nodeCount; i++)
            {
                tree.Delete(randomNumbers[i]);

                tree.Root.VerifyCount();
                HuaTuo.NUnit.Framework.Assert.IsTrue(tree.Root.IsBinarySearchTree(int.MinValue, int.MaxValue));

                var actualHeight = tree.GetHeight();

                //http://stackoverflow.com/questions/30769383/finding-the-minimum-and-maximum-height-in-a-avl-tree-given-a-number-of-nodes
                var maxHeight = 1.44 * Math.Log(nodeCount + 2, 2) - 0.328;

                HuaTuo.NUnit.Framework.Assert.IsTrue(actualHeight < maxHeight);

                HuaTuo.NUnit.Framework.Assert.IsTrue(tree.Count == nodeCount - 1 - i);
            }

            HuaTuo.NUnit.Framework.Assert.IsTrue(tree.Count == 0);
        }

        [HuaTuo.NUnit.Framework.Test]
        public void AVLTree_BulkInit_Test()
        {
            var nodeCount = 1000;

            var rnd = new Random();
            var randomNumbers = Enumerable.Range(1, nodeCount).ToList();

            var tree = new AVLTree<int>(randomNumbers, true);

            HuaTuo.NUnit.Framework.Assert.IsTrue(tree.Root.IsBinarySearchTree(int.MinValue, int.MaxValue));
            HuaTuo.NUnit.Framework.Assert.AreEqual(tree.Count, tree.Count());

            tree.Root.VerifyCount();

            for (int i = 0; i < nodeCount; i++)
            {
                tree.Delete(randomNumbers[i]);

                tree.Root.VerifyCount();
                HuaTuo.NUnit.Framework.Assert.IsTrue(tree.Root.IsBinarySearchTree(int.MinValue, int.MaxValue));

                var actualHeight = tree.GetHeight();

                //http://stackoverflow.com/questions/30769383/finding-the-minimum-and-maximum-height-in-a-avl-tree-given-a-number-of-nodes
                var maxHeight = 1.44 * Math.Log(nodeCount + 2, 2) - 0.328;

                HuaTuo.NUnit.Framework.Assert.IsTrue(actualHeight < maxHeight);

                HuaTuo.NUnit.Framework.Assert.IsTrue(tree.Count == nodeCount - 1 - i);
            }

            HuaTuo.NUnit.Framework.Assert.IsTrue(tree.Count == 0);
        }

        [HuaTuo.NUnit.Framework.Test]
        public void AVLTree_Stress_Test()
        {
            var nodeCount = 1000 * 10;

            var rnd = new Random();
            var randomNumbers = Enumerable.Range(1, nodeCount)
                                .OrderBy(x => rnd.Next())
                                .ToList();

            var tree = new AVLTree<int>();

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

            for (int i = 0; i < nodeCount; i++)
            {
                tree.Delete(randomNumbers[i]);
                HuaTuo.NUnit.Framework.Assert.IsTrue(tree.Count == nodeCount - 1 - i);
            }

            HuaTuo.NUnit.Framework.Assert.IsTrue(tree.Count == 0);
        }
    }
}
