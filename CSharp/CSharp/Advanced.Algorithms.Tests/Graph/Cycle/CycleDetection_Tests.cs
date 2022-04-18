using Advanced.Algorithms.Graph;


namespace Advanced.Algorithms.Tests.Graph
{
    
    public class CycleDetection_Tests
    {
        [NUnit.Framework.Test]
        public void Graph_Cycle_Detection_AdjancencyListGraph_Tests()
        {
            var graph = new Advanced.Algorithms.DataStructures.Graph.AdjacencyList.DiGraph<char>();

            graph.AddVertex('A');
            graph.AddVertex('B');
            graph.AddVertex('C');
            graph.AddVertex('D');
            graph.AddVertex('E');
            graph.AddVertex('F');
            graph.AddVertex('G');
            graph.AddVertex('H');


            graph.AddEdge('A', 'B');
            graph.AddEdge('B', 'C');
            graph.AddEdge('C', 'A');

            graph.AddEdge('C', 'D');
            graph.AddEdge('D', 'E');

            graph.AddEdge('E', 'F');
            graph.AddEdge('F', 'G');
            graph.AddEdge('G', 'E');

            graph.AddEdge('F', 'H');

            var algorithm = new CycleDetector<char>();

            NUnit.Framework.Assert.IsTrue(algorithm.HasCycle(graph));

            graph.RemoveEdge('C', 'A');
            graph.RemoveEdge('G', 'E');

            NUnit.Framework.Assert.IsFalse(algorithm.HasCycle(graph));
        }

        [NUnit.Framework.Test]
        public void Graph_Cycle_Detection_AdjancencyMatrixGraph_Tests()
        {
            var graph = new Advanced.Algorithms.DataStructures.Graph.AdjacencyMatrix.DiGraph<char>();

            graph.AddVertex('A');
            graph.AddVertex('B');
            graph.AddVertex('C');
            graph.AddVertex('D');
            graph.AddVertex('E');
            graph.AddVertex('F');
            graph.AddVertex('G');
            graph.AddVertex('H');


            graph.AddEdge('A', 'B');
            graph.AddEdge('B', 'C');
            graph.AddEdge('C', 'A');

            graph.AddEdge('C', 'D');
            graph.AddEdge('D', 'E');

            graph.AddEdge('E', 'F');
            graph.AddEdge('F', 'G');
            graph.AddEdge('G', 'E');

            graph.AddEdge('F', 'H');

            var algorithm = new CycleDetector<char>();

            NUnit.Framework.Assert.IsTrue(algorithm.HasCycle(graph));

            graph.RemoveEdge('C', 'A');
            graph.RemoveEdge('G', 'E');

            NUnit.Framework.Assert.IsFalse(algorithm.HasCycle(graph));
        }
    }
}
