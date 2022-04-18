using Advanced.Algorithms.DataStructures.Foundation;


namespace Advanced.Algorithms.Tests.DataStructures
{
    
    public class Stack_Tests
    {

        [NUnit.Framework.Test]
        public void ArrayStack_Test()
        {
            var stack = new Stack<string>();

            stack.Push("a");
            stack.Push("b");

            NUnit.Framework.Assert.AreEqual(stack.Count, 2);
            NUnit.Framework.Assert.AreEqual(stack.Peek(), "b");

            stack.Pop();

            NUnit.Framework.Assert.AreEqual(stack.Count, 1);
            NUnit.Framework.Assert.AreEqual(stack.Peek(), "a");

            stack.Pop();

            NUnit.Framework.Assert.AreEqual(stack.Count, 0);

            stack.Push("a");
            NUnit.Framework.Assert.AreEqual(stack.Count, 1);
            NUnit.Framework.Assert.AreEqual(stack.Peek(), "a");
        }

        [NUnit.Framework.Test]
        public void LinkedListStack_Test()
        {
            var stack = new Stack<string>(StackType.LinkedList);

            stack.Push("a");
            stack.Push("b");

            NUnit.Framework.Assert.AreEqual(stack.Count, 2);
            NUnit.Framework.Assert.AreEqual(stack.Peek(), "b");

            stack.Pop();

            NUnit.Framework.Assert.AreEqual(stack.Count, 1);
            NUnit.Framework.Assert.AreEqual(stack.Peek(), "a");

            stack.Pop();

            NUnit.Framework.Assert.AreEqual(stack.Count, 0);

            stack.Push("a");
            NUnit.Framework.Assert.AreEqual(stack.Count, 1);
            NUnit.Framework.Assert.AreEqual(stack.Peek(), "a");
        }
    }
}
