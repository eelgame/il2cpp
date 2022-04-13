using DataStructures.Trees;
using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Fact = NUnit.Framework.TestAttribute;

namespace UnitTest.DataStructuresTests
{
    public static class TernarySearchTreeTest
    {

        //         c
        //       / | \
        //      a  u  h
        //      |  |  | \
        //      t  t  e u
        //     /  / |  / |
        //    s  p  e i  s
        [Fact]
        public static void DoTest()
        {
            string[] words = new string[] { "cute", "cup", "at", "as", "he", "us", "i" };

            TernarySearchTree tree = new TernarySearchTree();

            tree.Insert(words);

          Assert.AreEqual('c', tree.Root.Value);
          Assert.AreEqual('h', tree.Root.GetRightChild.Value);
          Assert.AreEqual('e', tree.Root.GetRightChild.GetMiddleChild.Value);
          Assert.AreEqual('p', tree.Root.GetMiddleChild.GetMiddleChild.GetLeftChild.Value);
          Assert.AreEqual('s', tree.Root.GetLeftChild.GetMiddleChild.GetLeftChild.Value);

        }
    }
}
