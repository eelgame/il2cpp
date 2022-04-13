using System.Collections.Generic;
using DataStructures.Trees;
using NUnit.Framework;
using Fact = NUnit.Framework.TestAttribute;

namespace UnitTest.DataStructuresTests
{
    public static class BTreeTest
    {
        [Fact]
        public static void DoInsertTest()
        {
            BTree<int> bTree = new BTree<int>(4);

            //
            // CASE #1
            // Insert: 10, 30, 20, 50, 40, 60, 70
            // ROOT contains all values; no split.
            //
            /***************************************
             **
             **    [10 , 20 , 30 , 40, 50, 60, 70]
             **
             ***************************************
             */
            bTree.Insert(10);
            bTree.Insert(30);
            bTree.Insert(20);
            bTree.Insert(50);
            bTree.Insert(40);
            bTree.Insert(60);
            bTree.Insert(70);

          Assert.AreEqual(7, bTree.Root.Keys.Count);


            //
            // CASE #2
            // Insert to the previous tree: 35.
            // Split into multiple.
            //
            /***************************************
             **
             **                      [40]
             **                     /    \
             **                    /      \
             ** [10 , 20 , 30 , 35]        [50 , 60 , 70]
             **
             ***************************************
             */
            bTree.Insert(35);
          Assert.AreEqual(40, bTree.Root.Keys[0]);
          Assert.AreEqual(4, bTree.Root.Children[0].Keys.Count);
          Assert.AreEqual(3, bTree.Root.Children[1].Keys.Count);


            //
            // CASE #3
            // Insert to the previous tree: 5, 15, 25, 39.
            // Split leftmost child.
            //
            /***************************************
             **
             **                      [20 , 40]
             **                     /    |    \
             **                    /     |     \
             **         [5, 10, 15]      |      [50 , 60 , 70]
             **                          |
             **                   [25, 30, 35, 39]
             **
             ***************************************
             */

            bTree.Insert(5);
            bTree.Insert(15);
            bTree.Insert(25);
            bTree.Insert(39);
          Assert.AreEqual(20, bTree.Root.Keys[0]);
          Assert.AreEqual(40, bTree.Root.Keys[1]);
          Assert.AreEqual(3, bTree.Root.Children[0].Keys.Count);
          Assert.AreEqual(4, bTree.Root.Children[1].Keys.Count);
          Assert.AreEqual(3, bTree.Root.Children[2].Keys.Count);
        }

        [Fact]
        public static void DoSearchTest()
        {
            // Build a base tree
            BTree<int> bTree = new BTree<int>(4);
            bTree.Insert(10);
            bTree.Insert(30);
            bTree.Insert(20);
            bTree.Insert(50);
            bTree.Insert(40);
            bTree.Insert(60);
            bTree.Insert(70);
            bTree.Insert(35);
            bTree.Insert(5);
            bTree.Insert(15);
            bTree.Insert(25);
            bTree.Insert(39);

            // The tree now looks like this:
            /***************************************
             **
             **                      [20 , 40]
             **                     /    |    \
             **                    /     |     \
             **         [5, 10, 15]      |      [50 , 60 , 70]
             **                          |
             **                   [25, 30, 35, 39]
             **
             ***************************************
             */

          Assert.AreEqual(2, bTree.Search(20).Keys.Count);
          Assert.AreEqual(2, bTree.Search(40).Keys.Count);
            Assert.Null(bTree.Search(41));
          Assert.AreEqual(3, bTree.Search(5).Keys.Count);
          Assert.AreEqual(4, bTree.Search(25).Keys.Count);
        }

        [Fact]
        public static void DoDeleteTest()
        {
            // Build a base tree
            BTree<int> bTree = new BTree<int>(4);
            bTree.Insert(10);
            bTree.Insert(30);
            bTree.Insert(20);
            bTree.Insert(50);
            bTree.Insert(40);
            bTree.Insert(60);
            bTree.Insert(70);
            bTree.Insert(35);
            bTree.Insert(5);
            bTree.Insert(15);
            bTree.Insert(25);
            bTree.Insert(39);

            // The tree now looks like this:
            /***************************************
             **
             **                      [20 , 40]
             **                     /    |    \
             **                    /     |     \
             **         [5, 10, 15]      |      [50 , 60 , 70]
             **                          |
             **                   [25, 30, 35, 39]
             **
             ***************************************
             */
            // First. assert the shape.
          Assert.AreEqual(2, bTree.Search(20).Keys.Count);
          Assert.AreEqual(2, bTree.Search(40).Keys.Count);
            Assert.Null(bTree.Search(41));
          Assert.AreEqual(3, bTree.Search(5).Keys.Count);
          Assert.AreEqual(4, bTree.Search(25).Keys.Count);

            // Now, remove a key from the left-most child.
            bTree.Remove(5);

            // The tree now looks like this:
            /***************************************
             **
             **                              [40]
             **                             /    \
             **                            /      \
             ** [10, 15, 20, 25, 30, 35, 39]       [50 , 60 , 70]
             ** 
             ***************************************
             */

            // The tree should now be rooted around 40, with the left child full.
          Assert.AreEqual(null, bTree.Search(5));
          Assert.AreEqual(2, bTree.Root.Children.Count);
          Assert.AreEqual(7, bTree.Root.Children[0].Keys.Count); // left-most
          Assert.AreEqual(3, bTree.Root.Children[1].Keys.Count); // right-most

            // Remove 50 - it needs to be rebalanced now.
            bTree.Remove(50);

            // The tree now looks like this:
            /***************************************
             **
             **                           [39]
             **                          /    \
             **                         /      \
             ** [10, 15, 20, 25, 30, 35]       [40, 60, 70]
             ** 
             ***************************************
             */
           Assert.AreEqual(39, bTree.Root.Keys[0]);
           Assert.AreEqual(6, bTree.Root.Children[0].Keys.Count);
           Assert.AreEqual(3, bTree.Root.Children[1].Keys.Count);

             // Remove everything
             bTree.Remove(10);
             bTree.Remove(15);
             bTree.Remove(20);
             bTree.Remove(25);
             bTree.Remove(30);
             bTree.Remove(35);
             bTree.Remove(39);
             bTree.Remove(40);
             bTree.Remove(60);
             bTree.Remove(70);

             Assert.Null(bTree.Root);
        }
    }
}
