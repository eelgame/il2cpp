using Advanced.Algorithms.DataStructures;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Advanced.Algorithms.Tests.DataStructures
{
    
    public class TernarySearchTree_Tests
    {
        [HuaTuo.NUnit.Framework.Test]
        public void TernarySearchTree_Smoke_Test()
        {
            var tree = new TernarySearchTree<char>();

            tree.Insert("cat".ToCharArray());

            //IEnumerable test
            HuaTuo.NUnit.Framework.Assert.AreEqual(tree.Count, tree.Count());

            tree.Insert("cats".ToCharArray());

            //IEnumerable test
            HuaTuo.NUnit.Framework.Assert.AreEqual(tree.Count, tree.Count());

            tree.Insert("cut".ToCharArray());

            //IEnumerable test
            HuaTuo.NUnit.Framework.Assert.AreEqual(tree.Count, tree.Count());

            tree.Insert("cuts".ToCharArray());

            //IEnumerable test
            HuaTuo.NUnit.Framework.Assert.AreEqual(tree.Count, tree.Count());

            tree.Insert("up".ToCharArray());

            //IEnumerable test
            HuaTuo.NUnit.Framework.Assert.AreEqual(tree.Count, tree.Count());

            tree.Insert("bug".ToCharArray());

            //IEnumerable test
            HuaTuo.NUnit.Framework.Assert.AreEqual(tree.Count, tree.Count());

            tree.Insert("bugs".ToCharArray());

            //IEnumerable test
            HuaTuo.NUnit.Framework.Assert.AreEqual(tree.Count, tree.Count());

            //IEnumerable test
            HuaTuo.NUnit.Framework.Assert.AreEqual(tree.Count, tree.Count());

            HuaTuo.NUnit.Framework.Assert.IsTrue(tree.Contains("cat".ToCharArray()));
            HuaTuo.NUnit.Framework.Assert.IsTrue(tree.Contains("cut".ToCharArray()));
            HuaTuo.NUnit.Framework.Assert.IsFalse(tree.Contains("bu".ToCharArray()));
            HuaTuo.NUnit.Framework.Assert.IsTrue(tree.ContainsPrefix("bu".ToCharArray()));

            tree.Delete("cuts".ToCharArray());
            HuaTuo.NUnit.Framework.Assert.IsFalse(tree.Contains("cuts".ToCharArray()));

            var matches = tree.StartsWith("u".ToCharArray());
            HuaTuo.NUnit.Framework.Assert.IsTrue(matches.Count == 1);

            //IEnumerable test
            HuaTuo.NUnit.Framework.Assert.AreEqual(tree.Count, tree.Count());

            matches = tree.StartsWith("cu".ToCharArray());
            HuaTuo.NUnit.Framework.Assert.IsTrue(matches.Count == 1);

            matches = tree.StartsWith("bu".ToCharArray());
            HuaTuo.NUnit.Framework.Assert.IsTrue(matches.Count == 2);

            matches = tree.StartsWith("c".ToCharArray());
            HuaTuo.NUnit.Framework.Assert.IsTrue(matches.Count == 3);

            matches = tree.StartsWith("ca".ToCharArray());
            HuaTuo.NUnit.Framework.Assert.IsTrue(matches.Count == 2);

            tree.Delete("cats".ToCharArray());

            //IEnumerable test
            HuaTuo.NUnit.Framework.Assert.AreEqual(tree.Count, tree.Count());


            tree.Delete("up".ToCharArray());

            //IEnumerable test
            HuaTuo.NUnit.Framework.Assert.AreEqual(tree.Count, tree.Count());

            tree.Delete("bug".ToCharArray());

            //IEnumerable test
            HuaTuo.NUnit.Framework.Assert.AreEqual(tree.Count, tree.Count());

            tree.Delete("bugs".ToCharArray());

            //IEnumerable test
            HuaTuo.NUnit.Framework.Assert.AreEqual(tree.Count, tree.Count());

            tree.Delete("cat".ToCharArray());

            //IEnumerable test
            HuaTuo.NUnit.Framework.Assert.AreEqual(tree.Count, tree.Count());

            tree.Delete("cut".ToCharArray());

            //IEnumerable test
            HuaTuo.NUnit.Framework.Assert.AreEqual(tree.Count, tree.Count());
        }

        [HuaTuo.NUnit.Framework.Test]
        public void TernarySearchTree_Test()
        {
            var tree = new TernarySearchTree<char>();

            var testCount = 1000;

            var testStrings = new List<string>();

            while (testCount > 0)
            {
                var testString = randomString(3);
                testStrings.Add(testString);
                testCount--;
            }

            testStrings = new List<string>(testStrings.Distinct());

            foreach (var testString in testStrings)
            {
                tree.Insert(testString.ToArray());
            }

            //IEnumerable test
            HuaTuo.NUnit.Framework.Assert.AreEqual(tree.Count, tree.Count());

            foreach (var item in tree)
            {
                var existing = string.Join("", item);
                HuaTuo.NUnit.Framework.Assert.IsTrue(testStrings.Contains(existing));
            }

            foreach (var item in testStrings)
            {
                HuaTuo.NUnit.Framework.Assert.IsTrue(tree.Contains(item.ToArray()));
            }

            foreach (var testString in testStrings)
            {
                tree.Delete(testString.ToArray());
            }

        }

        private static Random random = new Random();

        public static string randomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return string.Join("", Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }

}
