using Advanced.Algorithms.DataStructures.Foundation;


namespace Advanced.Algorithms.Tests.DataStructures
{
    
    public class Queue_Tests
    {

        [NUnit.Framework.Test]
        public void ArrayQueue_Test()
        {
            var Queue = new Queue<string>();

            Queue.Enqueue("a");
            Queue.Enqueue("b");
            Queue.Enqueue("c");

            NUnit.Framework.Assert.AreEqual(Queue.Count, 3);
            NUnit.Framework.Assert.AreEqual(Queue.Dequeue(), "a");


            NUnit.Framework.Assert.AreEqual(Queue.Count, 2);
            NUnit.Framework.Assert.AreEqual(Queue.Dequeue(), "b");

            NUnit.Framework.Assert.AreEqual(Queue.Count, 1);
            NUnit.Framework.Assert.AreEqual(Queue.Dequeue(), "c");

            NUnit.Framework.Assert.AreEqual(Queue.Count, 0);

            Queue.Enqueue("a");

            NUnit.Framework.Assert.AreEqual(Queue.Count, 1);
            NUnit.Framework.Assert.AreEqual(Queue.Dequeue(), "a");

        }

        [NUnit.Framework.Test]
        public void LinkedListQueue_Test()
        {
            var Queue = new Queue<string>(QueueType.LinkedList);

            Queue.Enqueue("a");
            Queue.Enqueue("b");
            Queue.Enqueue("c");

            NUnit.Framework.Assert.AreEqual(Queue.Count, 3);
            NUnit.Framework.Assert.AreEqual(Queue.Dequeue(), "a");


            NUnit.Framework.Assert.AreEqual(Queue.Count, 2);
            NUnit.Framework.Assert.AreEqual(Queue.Dequeue(), "b");

            NUnit.Framework.Assert.AreEqual(Queue.Count, 1);
            NUnit.Framework.Assert.AreEqual(Queue.Dequeue(), "c");

            NUnit.Framework.Assert.AreEqual(Queue.Count, 0);

            Queue.Enqueue("a");

            NUnit.Framework.Assert.AreEqual(Queue.Count, 1);
            NUnit.Framework.Assert.AreEqual(Queue.Dequeue(), "a");

        }
    }
}
