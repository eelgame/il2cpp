using Advanced.Algorithms.DataStructures;

using System.Linq;

namespace Advanced.Algorithms.Tests.DataStructures
{
    
    public class Trie_Tests
    {
        [NUnit.Framework.Test]
        public void Trie_Smoke_Test_Banana()
        {
            var trie = new Trie<char>();

            trie.Insert("banana".ToCharArray());
            NUnit.Framework.Assert.IsTrue(trie.Contains("banana".ToCharArray()));

            trie.Insert("anana".ToCharArray());
            NUnit.Framework.Assert.IsTrue(trie.Contains("anana".ToCharArray()));

            trie.Insert("nana".ToCharArray());
            NUnit.Framework.Assert.IsTrue(trie.Contains("nana".ToCharArray()));
            NUnit.Framework.Assert.IsFalse(trie.Contains("banan".ToCharArray()));
            NUnit.Framework.Assert.IsTrue(trie.ContainsPrefix("banan".ToCharArray()));

            trie.Insert("ana".ToCharArray());
            NUnit.Framework.Assert.IsTrue(trie.Contains("ana".ToCharArray()));

            trie.Insert("na".ToCharArray());
            NUnit.Framework.Assert.IsTrue(trie.Contains("na".ToCharArray()));

            trie.Insert("a".ToCharArray());
            NUnit.Framework.Assert.IsTrue(trie.Contains("a".ToCharArray()));
            NUnit.Framework.Assert.IsTrue(trie.Count == 6);

            //IEnumerable test
            NUnit.Framework.Assert.AreEqual(trie.Count, trie.Count());

            NUnit.Framework.Assert.IsTrue(trie.Contains("banana".ToCharArray()));
            trie.Delete("banana".ToCharArray());
            NUnit.Framework.Assert.IsFalse(trie.Contains("banana".ToCharArray()));

            NUnit.Framework.Assert.IsTrue(trie.Contains("anana".ToCharArray()));
            trie.Delete("anana".ToCharArray());
            NUnit.Framework.Assert.IsFalse(trie.Contains("anana".ToCharArray()));

            NUnit.Framework.Assert.IsTrue(trie.Contains("nana".ToCharArray()));
            trie.Delete("nana".ToCharArray());
            NUnit.Framework.Assert.IsFalse(trie.Contains("nana".ToCharArray()));

            NUnit.Framework.Assert.IsTrue(trie.Contains("ana".ToCharArray()));
            trie.Delete("ana".ToCharArray());
            NUnit.Framework.Assert.IsFalse(trie.Contains("ana".ToCharArray()));

            NUnit.Framework.Assert.IsTrue(trie.Contains("na".ToCharArray()));
            trie.Delete("na".ToCharArray());
            NUnit.Framework.Assert.IsFalse(trie.Contains("na".ToCharArray()));

            NUnit.Framework.Assert.IsTrue(trie.Contains("a".ToCharArray()));
            trie.Delete("a".ToCharArray());
            NUnit.Framework.Assert.IsFalse(trie.Contains("a".ToCharArray()));
            NUnit.Framework.Assert.IsTrue(trie.Count == 0);

            //IEnumerable test
            NUnit.Framework.Assert.AreEqual(trie.Count, trie.Count());
        }

        /// <summary>
        /// A tree test
        /// </summary>
        [NUnit.Framework.Test]
        public void Trie_Smoke_Test()
        {
            var trie = new Trie<char>();

            trie.Insert("abcd".ToCharArray());
            trie.Insert("abcde".ToCharArray());
            trie.Insert("bcde".ToCharArray());
            trie.Insert("cdab".ToCharArray());
            trie.Insert("efghi".ToCharArray());

            NUnit.Framework.Assert.IsTrue(trie.Contains("cdab".ToCharArray()));
            NUnit.Framework.Assert.IsFalse(trie.Contains("ab".ToCharArray()));

            trie.Delete("cdab".ToCharArray());
            NUnit.Framework.Assert.IsFalse(trie.Contains("cdab".ToCharArray()));

            //IEnumerable test
            NUnit.Framework.Assert.AreEqual(trie.Count, trie.Count());

            var matches = trie.StartsWith("b".ToCharArray());
            NUnit.Framework.Assert.IsTrue(matches.Count == 1);

            matches = trie.StartsWith("abcd".ToCharArray());
            NUnit.Framework.Assert.IsTrue(matches.Count == 2);

            trie.Delete("abcd".ToCharArray());
            trie.Delete("abcde".ToCharArray());
            trie.Delete("bcde".ToCharArray());
            trie.Delete("efghi".ToCharArray());

            //IEnumerable test
            NUnit.Framework.Assert.AreEqual(trie.Count, trie.Count());

        }
    }
}
