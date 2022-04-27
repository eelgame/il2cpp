using Advanced.Algorithms.DataStructures;

using System.Linq;

namespace Advanced.Algorithms.Tests.DataStructures
{
    
    public class Trie_Tests
    {
        [HuaTuo.NUnit.Framework.Test]
        public void Trie_Smoke_Test_Banana()
        {
            var trie = new Trie<char>();

            trie.Insert("banana".ToCharArray());
            HuaTuo.NUnit.Framework.Assert.IsTrue(trie.Contains("banana".ToCharArray()));

            trie.Insert("anana".ToCharArray());
            HuaTuo.NUnit.Framework.Assert.IsTrue(trie.Contains("anana".ToCharArray()));

            trie.Insert("nana".ToCharArray());
            HuaTuo.NUnit.Framework.Assert.IsTrue(trie.Contains("nana".ToCharArray()));
            HuaTuo.NUnit.Framework.Assert.IsFalse(trie.Contains("banan".ToCharArray()));
            HuaTuo.NUnit.Framework.Assert.IsTrue(trie.ContainsPrefix("banan".ToCharArray()));

            trie.Insert("ana".ToCharArray());
            HuaTuo.NUnit.Framework.Assert.IsTrue(trie.Contains("ana".ToCharArray()));

            trie.Insert("na".ToCharArray());
            HuaTuo.NUnit.Framework.Assert.IsTrue(trie.Contains("na".ToCharArray()));

            trie.Insert("a".ToCharArray());
            HuaTuo.NUnit.Framework.Assert.IsTrue(trie.Contains("a".ToCharArray()));
            HuaTuo.NUnit.Framework.Assert.IsTrue(trie.Count == 6);

            //IEnumerable test
            HuaTuo.NUnit.Framework.Assert.AreEqual(trie.Count, trie.Count());

            HuaTuo.NUnit.Framework.Assert.IsTrue(trie.Contains("banana".ToCharArray()));
            trie.Delete("banana".ToCharArray());
            HuaTuo.NUnit.Framework.Assert.IsFalse(trie.Contains("banana".ToCharArray()));

            HuaTuo.NUnit.Framework.Assert.IsTrue(trie.Contains("anana".ToCharArray()));
            trie.Delete("anana".ToCharArray());
            HuaTuo.NUnit.Framework.Assert.IsFalse(trie.Contains("anana".ToCharArray()));

            HuaTuo.NUnit.Framework.Assert.IsTrue(trie.Contains("nana".ToCharArray()));
            trie.Delete("nana".ToCharArray());
            HuaTuo.NUnit.Framework.Assert.IsFalse(trie.Contains("nana".ToCharArray()));

            HuaTuo.NUnit.Framework.Assert.IsTrue(trie.Contains("ana".ToCharArray()));
            trie.Delete("ana".ToCharArray());
            HuaTuo.NUnit.Framework.Assert.IsFalse(trie.Contains("ana".ToCharArray()));

            HuaTuo.NUnit.Framework.Assert.IsTrue(trie.Contains("na".ToCharArray()));
            trie.Delete("na".ToCharArray());
            HuaTuo.NUnit.Framework.Assert.IsFalse(trie.Contains("na".ToCharArray()));

            HuaTuo.NUnit.Framework.Assert.IsTrue(trie.Contains("a".ToCharArray()));
            trie.Delete("a".ToCharArray());
            HuaTuo.NUnit.Framework.Assert.IsFalse(trie.Contains("a".ToCharArray()));
            HuaTuo.NUnit.Framework.Assert.IsTrue(trie.Count == 0);

            //IEnumerable test
            HuaTuo.NUnit.Framework.Assert.AreEqual(trie.Count, trie.Count());
        }

        /// <summary>
        /// A tree test
        /// </summary>
        [HuaTuo.NUnit.Framework.Test]
        public void Trie_Smoke_Test()
        {
            var trie = new Trie<char>();

            trie.Insert("abcd".ToCharArray());
            trie.Insert("abcde".ToCharArray());
            trie.Insert("bcde".ToCharArray());
            trie.Insert("cdab".ToCharArray());
            trie.Insert("efghi".ToCharArray());

            HuaTuo.NUnit.Framework.Assert.IsTrue(trie.Contains("cdab".ToCharArray()));
            HuaTuo.NUnit.Framework.Assert.IsFalse(trie.Contains("ab".ToCharArray()));

            trie.Delete("cdab".ToCharArray());
            HuaTuo.NUnit.Framework.Assert.IsFalse(trie.Contains("cdab".ToCharArray()));

            //IEnumerable test
            HuaTuo.NUnit.Framework.Assert.AreEqual(trie.Count, trie.Count());

            var matches = trie.StartsWith("b".ToCharArray());
            HuaTuo.NUnit.Framework.Assert.IsTrue(matches.Count == 1);

            matches = trie.StartsWith("abcd".ToCharArray());
            HuaTuo.NUnit.Framework.Assert.IsTrue(matches.Count == 2);

            trie.Delete("abcd".ToCharArray());
            trie.Delete("abcde".ToCharArray());
            trie.Delete("bcde".ToCharArray());
            trie.Delete("efghi".ToCharArray());

            //IEnumerable test
            HuaTuo.NUnit.Framework.Assert.AreEqual(trie.Count, trie.Count());

        }
    }
}
