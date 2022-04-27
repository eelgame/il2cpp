using Advanced.Algorithms.DataStructures;

using System.Linq;

namespace Advanced.Algorithms.Tests.DataStructures
{
    
    public class CircularLinkedList_Tests
    {
        /// <summary>
        /// doubly linked list tests 
        /// </summary>
        [HuaTuo.NUnit.Framework.Test]
        public void CircularLinkedList_Test()
        {
            var list = new CircularLinkedList<string>();

            list.Insert("a");
            list.Insert("b");
            list.Insert("c");
            list.Insert("c");

            HuaTuo.NUnit.Framework.Assert.AreEqual(list.Count(), 4);

            list.Delete("a");
            HuaTuo.NUnit.Framework.Assert.AreEqual(list.Count(), 3);

            list.Delete("b");
            HuaTuo.NUnit.Framework.Assert.AreEqual(list.Count(), 2);

            list.Delete("c");
            HuaTuo.NUnit.Framework.Assert.AreEqual(list.Count(), 1);

            list.Insert("a");
            HuaTuo.NUnit.Framework.Assert.AreEqual(list.Count(), 2);

            list.Delete("a");
            HuaTuo.NUnit.Framework.Assert.AreEqual(list.Count(), 1);

            list.Delete("c");
            HuaTuo.NUnit.Framework.Assert.AreEqual(list.Count(), 0);

            list.Insert("a");
            list.Insert("b");
            list.Insert("c");
            list.Insert("c");

            HuaTuo.NUnit.Framework.Assert.AreEqual(list.Count(), 4);

            list.Delete("a");
            HuaTuo.NUnit.Framework.Assert.AreEqual(list.Count(), 3);

            list.Delete("b");
            HuaTuo.NUnit.Framework.Assert.AreEqual(list.Count(), 2);

            list.Delete("c");
            HuaTuo.NUnit.Framework.Assert.AreEqual(list.Count(), 1);

            list.Insert("a");
            HuaTuo.NUnit.Framework.Assert.AreEqual(list.Count(), 2);

            list.Delete("a");
            HuaTuo.NUnit.Framework.Assert.AreEqual(list.Count(), 1);

            list.Delete("c");
            HuaTuo.NUnit.Framework.Assert.AreEqual(list.Count(), 0);
        }
    }
}
