using Advanced.Algorithms.Distributed;


namespace Advanced.Algorithms.Tests
{
    
    public class CircularQueue_Tests
    {

        [NUnit.Framework.Test]
        public void CircularQueue_Test()
        {
            var Queue = new CircularQueue<int>(7);

            NUnit.Framework.Assert.AreEqual(0, Queue.Enqueue(1));
            NUnit.Framework.Assert.AreEqual(0, Queue.Enqueue(2));

            NUnit.Framework.Assert.AreEqual(0, Queue.Enqueue(3));
            NUnit.Framework.Assert.AreEqual(0, Queue.Enqueue(4));
            NUnit.Framework.Assert.AreEqual(0, Queue.Enqueue(5));
            NUnit.Framework.Assert.AreEqual(0, Queue.Enqueue(6));
            NUnit.Framework.Assert.AreEqual(0, Queue.Enqueue(7));
            NUnit.Framework.Assert.AreEqual(1, Queue.Enqueue(8));
            NUnit.Framework.Assert.AreEqual(2, Queue.Enqueue(9));

            NUnit.Framework.Assert.AreEqual(Queue.Count, 7);
            NUnit.Framework.Assert.AreEqual(3, Queue.Dequeue());

            NUnit.Framework.Assert.AreEqual(Queue.Count, 6);
            NUnit.Framework.Assert.AreEqual(Queue.Dequeue(), 4);

            NUnit.Framework.Assert.AreEqual(Queue.Count, 5);
            NUnit.Framework.Assert.AreEqual(Queue.Dequeue(), 5);

            NUnit.Framework.Assert.AreEqual(Queue.Count, 4);
            NUnit.Framework.Assert.AreEqual(Queue.Dequeue(), 6);

            NUnit.Framework.Assert.AreEqual(Queue.Count, 3);
            NUnit.Framework.Assert.AreEqual(Queue.Dequeue(), 7);

            NUnit.Framework.Assert.AreEqual(Queue.Count, 2);
            NUnit.Framework.Assert.AreEqual(Queue.Dequeue(), 8);

            NUnit.Framework.Assert.AreEqual(Queue.Count, 1);
            NUnit.Framework.Assert.AreEqual(Queue.Dequeue(), 9);

            NUnit.Framework.Assert.AreEqual(Queue.Count, 0);

            NUnit.Framework.Assert.AreEqual(0, Queue.Enqueue(1));
            NUnit.Framework.Assert.AreEqual(0, Queue.Enqueue(2));

            NUnit.Framework.Assert.AreEqual(Queue.Count, 2);
            NUnit.Framework.Assert.AreEqual(1, Queue.Dequeue());

            NUnit.Framework.Assert.AreEqual(Queue.Count, 1);
            NUnit.Framework.Assert.AreEqual(Queue.Dequeue(), 2);
        }


    }
}