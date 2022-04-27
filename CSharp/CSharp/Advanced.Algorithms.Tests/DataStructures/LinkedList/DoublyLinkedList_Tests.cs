using Advanced.Algorithms.DataStructures;

using System.Linq;

namespace Advanced.Algorithms.Tests.DataStructures
{
    
    public class DoublyLinkedList_Tests
    {
        /// <summary>
        /// doubly linked list tests 
        /// </summary>
        [HuaTuo.NUnit.Framework.Test]
        public void DoublyLinkedList_Test()
        {
            var list = new DoublyLinkedList<string>();

            list.InsertFirst("a");
            list.InsertLast("b");
            list.InsertFirst("c");
            list.InsertLast("d");

            //{c,a,b,c}
            HuaTuo.NUnit.Framework.Assert.AreEqual(list.Count(), 4);
            HuaTuo.NUnit.Framework.Assert.AreEqual(list.Head.Data, "c");

            list.Delete("c");

            //{a,b,c}
            HuaTuo.NUnit.Framework.Assert.AreEqual(list.Count(), 3);
            HuaTuo.NUnit.Framework.Assert.AreEqual(list.Head.Data, "a");

            //{b}
            list.DeleteFirst();
            list.DeleteLast();

            HuaTuo.NUnit.Framework.Assert.AreEqual(list.Count(), 1);
            HuaTuo.NUnit.Framework.Assert.AreEqual(list.Head.Data, "b");

            list.Delete("b");
            HuaTuo.NUnit.Framework.Assert.AreEqual(list.Count(), 0);

            list.InsertFirst("a");
            list.InsertLast("a");
            list.InsertFirst("c");
            list.InsertLast("a");

            list.Delete("c");
            list.Delete("a");
            list.Delete("a");
            list.Delete("a");
            HuaTuo.NUnit.Framework.Assert.AreEqual(list.Count(), 0);
        }
    }
}
