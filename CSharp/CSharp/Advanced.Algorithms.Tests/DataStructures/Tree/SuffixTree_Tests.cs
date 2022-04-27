using Advanced.Algorithms.DataStructures;

using System.Linq;

namespace Advanced.Algorithms.Tests.DataStructures
{
    
    public class Suffix_Tests
    {
        /// <summary>
        /// A tree test
        /// </summary>
        [HuaTuo.NUnit.Framework.Test]
        public void Suffix_Smoke_Test()
        {
            var tree = new SuffixTree<char>();

            tree.Insert("bananaa".ToCharArray());
            HuaTuo.NUnit.Framework.Assert.IsTrue(tree.Count == 1);

            //IEnumerable test
            HuaTuo.NUnit.Framework.Assert.AreEqual(tree.Count, tree.Count());

            HuaTuo.NUnit.Framework.Assert.IsTrue(tree.Contains("aa".ToCharArray()));
            HuaTuo.NUnit.Framework.Assert.IsFalse(tree.Contains("ab".ToCharArray()));

            var matches = tree.StartsWith("na".ToCharArray());
            HuaTuo.NUnit.Framework.Assert.IsTrue(matches.Count == 2);

            matches = tree.StartsWith("an".ToCharArray());
            HuaTuo.NUnit.Framework.Assert.IsTrue(matches.Count == 2);

            tree.Delete("bananaa".ToCharArray());
            HuaTuo.NUnit.Framework.Assert.IsTrue(tree.Count == 0);

            //IEnumerable test
            HuaTuo.NUnit.Framework.Assert.AreEqual(tree.Count, tree.Count());
        }
    }
}
