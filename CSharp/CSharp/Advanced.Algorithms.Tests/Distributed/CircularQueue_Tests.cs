using Advanced.Algorithms.Distributed;


namespace Advanced.Algorithms.Tests
{
    
    public class CircularQueue_Tests
    {

        [HuaTuo.NUnit.Framework.Test]
        public void CircularQueue_Test()
        {
            var Queue = new CircularQueue<int>(7);

            HuaTuo.NUnit.Framework.Assert.AreEqual(0, Queue.Enqueue(1));
            HuaTuo.NUnit.Framework.Assert.AreEqual(0, Queue.Enqueue(2));

            HuaTuo.NUnit.Framework.Assert.AreEqual(0, Queue.Enqueue(3));
            HuaTuo.NUnit.Framework.Assert.AreEqual(0, Queue.Enqueue(4));
            HuaTuo.NUnit.Framework.Assert.AreEqual(0, Queue.Enqueue(5));
            HuaTuo.NUnit.Framework.Assert.AreEqual(0, Queue.Enqueue(6));
            HuaTuo.NUnit.Framework.Assert.AreEqual(0, Queue.Enqueue(7));
            HuaTuo.NUnit.Framework.Assert.AreEqual(1, Queue.Enqueue(8));
            HuaTuo.NUnit.Framework.Assert.AreEqual(2, Queue.Enqueue(9));

            HuaTuo.NUnit.Framework.Assert.AreEqual(Queue.Count, 7);
            HuaTuo.NUnit.Framework.Assert.AreEqual(3, Queue.Dequeue());

            HuaTuo.NUnit.Framework.Assert.AreEqual(Queue.Count, 6);
            HuaTuo.NUnit.Framework.Assert.AreEqual(Queue.Dequeue(), 4);

            HuaTuo.NUnit.Framework.Assert.AreEqual(Queue.Count, 5);
            HuaTuo.NUnit.Framework.Assert.AreEqual(Queue.Dequeue(), 5);

            HuaTuo.NUnit.Framework.Assert.AreEqual(Queue.Count, 4);
            HuaTuo.NUnit.Framework.Assert.AreEqual(Queue.Dequeue(), 6);

            HuaTuo.NUnit.Framework.Assert.AreEqual(Queue.Count, 3);
            HuaTuo.NUnit.Framework.Assert.AreEqual(Queue.Dequeue(), 7);

            HuaTuo.NUnit.Framework.Assert.AreEqual(Queue.Count, 2);
            HuaTuo.NUnit.Framework.Assert.AreEqual(Queue.Dequeue(), 8);

            HuaTuo.NUnit.Framework.Assert.AreEqual(Queue.Count, 1);
            HuaTuo.NUnit.Framework.Assert.AreEqual(Queue.Dequeue(), 9);

            HuaTuo.NUnit.Framework.Assert.AreEqual(Queue.Count, 0);

            HuaTuo.NUnit.Framework.Assert.AreEqual(0, Queue.Enqueue(1));
            HuaTuo.NUnit.Framework.Assert.AreEqual(0, Queue.Enqueue(2));

            HuaTuo.NUnit.Framework.Assert.AreEqual(Queue.Count, 2);
            HuaTuo.NUnit.Framework.Assert.AreEqual(1, Queue.Dequeue());

            HuaTuo.NUnit.Framework.Assert.AreEqual(Queue.Count, 1);
            HuaTuo.NUnit.Framework.Assert.AreEqual(Queue.Dequeue(), 2);
        }


    }
}