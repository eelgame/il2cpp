using Advanced.Algorithms.DataStructures;

using System.Linq;

namespace Advanced.Algorithms.Tests.DataStructures
{
    
    public class Tree_Tests
    {
        /// <summary>
        /// A tree test
        /// </summary>
        [NUnit.Framework.Test]
        public void Tree_Test()
        {

            var tree = new Tree<int>();
            NUnit.Framework.Assert.AreEqual(tree.GetHeight(), -1);

            tree.Insert(0, 0);
            NUnit.Framework.Assert.AreEqual(tree.GetHeight(), 0);

            tree.Insert(0, 1);
            NUnit.Framework.Assert.AreEqual(tree.GetHeight(), 1);

            tree.Insert(1, 2);
            NUnit.Framework.Assert.AreEqual(tree.GetHeight(), 2);

            //IEnumerable test using linq count()
            NUnit.Framework.Assert.AreEqual(tree.Count, tree.Count());

            tree.Delete(1);
            NUnit.Framework.Assert.AreEqual(tree.GetHeight(), 1);

            tree.Delete(2);
            NUnit.Framework.Assert.AreEqual(tree.GetHeight(), 0);

            tree.Delete(0);
            NUnit.Framework.Assert.AreEqual(tree.GetHeight(), -1);

            tree.Insert(0, 0);
            NUnit.Framework.Assert.AreEqual(tree.GetHeight(), 0);

            tree.Insert(0, 1);
            NUnit.Framework.Assert.AreEqual(tree.GetHeight(), 1);

            tree.Insert(1, 2);
            NUnit.Framework.Assert.AreEqual(tree.GetHeight(), 2);

            //IEnumerable test using linq count()
            NUnit.Framework.Assert.AreEqual(tree.Count, tree.Count());

        }
    }
}
