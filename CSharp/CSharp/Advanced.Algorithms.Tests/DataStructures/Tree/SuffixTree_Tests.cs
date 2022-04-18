using Advanced.Algorithms.DataStructures;

using System.Linq;

namespace Advanced.Algorithms.Tests.DataStructures
{
    
    public class Suffix_Tests
    {
        /// <summary>
        /// A tree test
        /// </summary>
        [NUnit.Framework.Test]
        public void Suffix_Smoke_Test()
        {
            var tree = new SuffixTree<char>();

            tree.Insert("bananaa".ToCharArray());
            NUnit.Framework.Assert.IsTrue(tree.Count == 1);

            //IEnumerable test
            NUnit.Framework.Assert.AreEqual(tree.Count, tree.Count());

            NUnit.Framework.Assert.IsTrue(tree.Contains("aa".ToCharArray()));
            NUnit.Framework.Assert.IsFalse(tree.Contains("ab".ToCharArray()));

            var matches = tree.StartsWith("na".ToCharArray());
            NUnit.Framework.Assert.IsTrue(matches.Count == 2);

            matches = tree.StartsWith("an".ToCharArray());
            NUnit.Framework.Assert.IsTrue(matches.Count == 2);

            tree.Delete("bananaa".ToCharArray());
            NUnit.Framework.Assert.IsTrue(tree.Count == 0);

            //IEnumerable test
            NUnit.Framework.Assert.AreEqual(tree.Count, tree.Count());
        }
    }
}
