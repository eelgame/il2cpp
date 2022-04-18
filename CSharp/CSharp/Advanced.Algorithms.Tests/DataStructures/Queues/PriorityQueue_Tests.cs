using Advanced.Algorithms.DataStructures;


namespace Advanced.Algorithms.Tests.DataStructures
{
    
    public class PriorityQueue_Tests
    {

        [NUnit.Framework.Test]
        public void Min_PriorityQueue_Test()
        {
            var queue = new PriorityQueue<int>();

            queue.Enqueue(10);
            queue.Enqueue(9);
            queue.Enqueue(1);
            queue.Enqueue(21);

            NUnit.Framework.Assert.AreEqual(queue.Dequeue(), 1);
            NUnit.Framework.Assert.AreEqual(queue.Dequeue(), 9);
            NUnit.Framework.Assert.AreEqual(queue.Dequeue(), 10);
            NUnit.Framework.Assert.AreEqual(queue.Dequeue(), 21);

        }

        [NUnit.Framework.Test]
        public void Max_PriorityQueue_Test()
        {
            var queue = new PriorityQueue<int>(SortDirection.Descending);

            queue.Enqueue(10);
            queue.Enqueue(9);
            queue.Enqueue(1);
            queue.Enqueue(21);

            NUnit.Framework.Assert.AreEqual(queue.Dequeue(), 21);
            NUnit.Framework.Assert.AreEqual(queue.Dequeue(), 10);
            NUnit.Framework.Assert.AreEqual(queue.Dequeue(), 9);
            NUnit.Framework.Assert.AreEqual(queue.Dequeue(), 1);

        }
    }
}
