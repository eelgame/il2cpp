using Advanced.Algorithms.DataStructures;

using System;
using System.Linq;

namespace Advanced.Algorithms.Tests.DataStructures
{
    
    public class BinaryTree_Tests
    {
        /// <summary>
        /// A tree test
        /// </summary>
        [HuaTuo.NUnit.Framework.Test]
        public void BinaryTree_Test()
        {
            var tree = new BinaryTree<int>();
            HuaTuo.NUnit.Framework.Assert.AreEqual(tree.GetHeight(), -1);

            tree.Insert(0, 0);
            HuaTuo.NUnit.Framework.Assert.AreEqual(tree.GetHeight(), 0);

            tree.Insert(0, 1);
            HuaTuo.NUnit.Framework.Assert.AreEqual(tree.GetHeight(), 1);

            tree.Insert(0, 2);
            HuaTuo.NUnit.Framework.Assert.AreEqual(tree.GetHeight(), 1);

            tree.Insert(1, 3);
            HuaTuo.NUnit.Framework.Assert.AreEqual(tree.GetHeight(), 2);

            try
            {
                tree.Delete(0);
            }
            catch (Exception e)
            {
                HuaTuo.NUnit.Framework.Assert.IsTrue(e.Message.StartsWith("Cannot delete two child node"));
            }

            //IEnumerable test using linq count()
            HuaTuo.NUnit.Framework.Assert.AreEqual(tree.Count, tree.Count());

            HuaTuo.NUnit.Framework.Assert.AreEqual(tree.GetHeight(), 2);

            tree.Delete(1);
            HuaTuo.NUnit.Framework.Assert.AreEqual(tree.GetHeight(), 1);

            tree.Delete(3);
            HuaTuo.NUnit.Framework.Assert.AreEqual(tree.GetHeight(), 1);

            tree.Delete(2);
            HuaTuo.NUnit.Framework.Assert.AreEqual(tree.GetHeight(), 0);

            tree.Delete(0);
            HuaTuo.NUnit.Framework.Assert.AreEqual(tree.GetHeight(), -1);

            tree.Insert(0, 0);
            HuaTuo.NUnit.Framework.Assert.AreEqual(tree.GetHeight(), 0);

            tree.Insert(0, 1);
            HuaTuo.NUnit.Framework.Assert.AreEqual(tree.GetHeight(), 1);

            tree.Insert(0, 2);
            HuaTuo.NUnit.Framework.Assert.AreEqual(tree.GetHeight(), 1);

            tree.Insert(1, 3);
            HuaTuo.NUnit.Framework.Assert.AreEqual(tree.GetHeight(), 2);

            //IEnumerable test using linq count()
            HuaTuo.NUnit.Framework.Assert.AreEqual(tree.Count, tree.Count());
        }
    }
}
