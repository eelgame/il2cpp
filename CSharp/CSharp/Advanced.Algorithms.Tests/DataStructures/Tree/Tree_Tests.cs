using Advanced.Algorithms.DataStructures;

using System.Linq;

namespace Advanced.Algorithms.Tests.DataStructures
{
    
    public class Tree_Tests
    {
        /// <summary>
        /// A tree test
        /// </summary>
        [HuaTuo.NUnit.Framework.Test]
        public void Tree_Test()
        {

            var tree = new Tree<int>();
            HuaTuo.NUnit.Framework.Assert.AreEqual(tree.GetHeight(), -1);

            tree.Insert(0, 0);
            HuaTuo.NUnit.Framework.Assert.AreEqual(tree.GetHeight(), 0);

            tree.Insert(0, 1);
            HuaTuo.NUnit.Framework.Assert.AreEqual(tree.GetHeight(), 1);

            tree.Insert(1, 2);
            HuaTuo.NUnit.Framework.Assert.AreEqual(tree.GetHeight(), 2);

            //IEnumerable test using linq count()
            HuaTuo.NUnit.Framework.Assert.AreEqual(tree.Count, tree.Count());

            tree.Delete(1);
            HuaTuo.NUnit.Framework.Assert.AreEqual(tree.GetHeight(), 1);

            tree.Delete(2);
            HuaTuo.NUnit.Framework.Assert.AreEqual(tree.GetHeight(), 0);

            tree.Delete(0);
            HuaTuo.NUnit.Framework.Assert.AreEqual(tree.GetHeight(), -1);

            tree.Insert(0, 0);
            HuaTuo.NUnit.Framework.Assert.AreEqual(tree.GetHeight(), 0);

            tree.Insert(0, 1);
            HuaTuo.NUnit.Framework.Assert.AreEqual(tree.GetHeight(), 1);

            tree.Insert(1, 2);
            HuaTuo.NUnit.Framework.Assert.AreEqual(tree.GetHeight(), 2);

            //IEnumerable test using linq count()
            HuaTuo.NUnit.Framework.Assert.AreEqual(tree.Count, tree.Count());

        }
    }
}
