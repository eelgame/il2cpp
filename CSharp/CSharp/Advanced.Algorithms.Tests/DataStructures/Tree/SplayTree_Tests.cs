using Advanced.Algorithms.DataStructures;

using System;
using System.Linq;

namespace Advanced.Algorithms.Tests.DataStructures
{
    
    public class SplayTree_Tests
    {
        /// <summary>
        /// A tree test
        /// </summary>
        [HuaTuo.NUnit.Framework.Test]
        public void SplayTree_Smoke_Test()
        {
            //insert test
            var tree = new SplayTree<int>();

            tree.Insert(11);
            tree.Insert(6);
            tree.Insert(8);
            tree.Insert(19);
            tree.Insert(4);
            tree.Insert(10);
            tree.Insert(5);
            tree.Insert(17);
            tree.Insert(43);
            tree.Insert(49);
            tree.Insert(31);

            //IEnumerable test using linq
            HuaTuo.NUnit.Framework.Assert.AreEqual(tree.Count, tree.Count());

            //delete
            tree.Delete(43);
            tree.Delete(11);
            tree.Delete(6);
            tree.Delete(8);
            tree.Delete(19);
            tree.Delete(4);
            tree.Delete(10);
            tree.Delete(5);
            tree.Delete(17);
            tree.Delete(49);
            tree.Delete(31);

            HuaTuo.NUnit.Framework.Assert.AreEqual(tree.Count, 0);

            tree.Insert(31);
        }


        [HuaTuo.NUnit.Framework.Test]
        public void SplayTree_BulkInit_Test()
        {
            var nodeCount = 1000;

            var rnd = new Random();
            var sortedNumbers = Enumerable.Range(1, nodeCount).ToList();

            var tree = new SplayTree<int>(sortedNumbers);

            HuaTuo.NUnit.Framework.Assert.IsTrue(tree.Root.IsBinarySearchTree(int.MinValue, int.MaxValue));
            HuaTuo.NUnit.Framework.Assert.AreEqual(tree.Count, tree.Count());
            tree.Root.VerifyCount();

            for (int i = 0; i < nodeCount; i++)
            {
                HuaTuo.NUnit.Framework.Assert.IsTrue(tree.Root.IsBinarySearchTree(int.MinValue, int.MaxValue));
                tree.Delete(sortedNumbers[i]);
                HuaTuo.NUnit.Framework.Assert.IsTrue(tree.Count == nodeCount - 1 - i);
            }

            HuaTuo.NUnit.Framework.Assert.IsTrue(tree.Count == 0);
        }

        [HuaTuo.NUnit.Framework.Test]
        public void SplayTree_Accuracy_Test()
        {
            var nodeCount = 1000;

            var rnd = new Random();
            var sorted = Enumerable.Range(1, nodeCount).ToList();
            var randomNumbers = sorted
                                .OrderBy(x => rnd.Next())
                                .ToList();

            var tree = new SplayTree<int>();

            for (int i = 0; i < nodeCount; i++)
            {
                tree.Insert(randomNumbers[i]);
                tree.Root.VerifyCount();
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
                    tree.Delete(randomNumbers[i]);
                }
                else
                {
                    var index = tree.IndexOf(randomNumbers[i]);
                    HuaTuo.NUnit.Framework.Assert.AreEqual(tree.ElementAt(index), randomNumbers[i]);
                    tree.RemoveAt(index);
                }

                tree.Root.VerifyCount();
                HuaTuo.NUnit.Framework.Assert.IsTrue(tree.Count == nodeCount - 1 - i);
            }

            HuaTuo.NUnit.Framework.Assert.IsTrue(tree.Count == 0);
        }

        [HuaTuo.NUnit.Framework.Test]
        public void SplayTree_Stress_Test()
        {
            var nodeCount = 1000 * 10;

            var rnd = new Random();
            var randomNumbers = Enumerable.Range(1, nodeCount)
                                .OrderBy(x => rnd.Next())
                                .ToList();

            var tree = new SplayTree<int>();

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
