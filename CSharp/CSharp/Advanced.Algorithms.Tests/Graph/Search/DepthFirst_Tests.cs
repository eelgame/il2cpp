using Advanced.Algorithms.Graph;



namespace Advanced.Algorithms.Tests.Graph
{
    
    public class DepthFirst_Tests
    {
        [HuaTuo.NUnit.Framework.Test]
        public void DepthFirst_AdjacencyListGraph_Smoke_Test()
        {
            var graph = new Advanced.Algorithms.DataStructures.Graph.AdjacencyList.Graph<char>();

            graph.AddVertex('A');
            graph.AddVertex('B');
            graph.AddVertex('C');
            graph.AddVertex('D');
            graph.AddVertex('E');

            graph.AddVertex('F');
            graph.AddVertex('G');
            graph.AddVertex('H');
            graph.AddVertex('I');

            graph.AddEdge('A', 'F');
            graph.AddEdge('B', 'F');
            graph.AddEdge('B', 'G');
            graph.AddEdge('C', 'H');
            graph.AddEdge('C', 'I');
            graph.AddEdge('D', 'G');
            graph.AddEdge('D', 'H');
            graph.AddEdge('E', 'F');
            graph.AddEdge('E', 'I');

            var algorithm = new DepthFirst<char>();

            HuaTuo.NUnit.Framework.Assert.IsTrue(algorithm.Find(graph, 'D'));

            HuaTuo.NUnit.Framework.Assert.IsFalse(algorithm.Find(graph, 'M'));

        }

        [HuaTuo.NUnit.Framework.Test]
        public void DepthFirst_AdjacencyMatrixGraph_Smoke_Test()
        {
            var graph = new Advanced.Algorithms.DataStructures.Graph.AdjacencyMatrix.Graph<char>();

            graph.AddVertex('A');
            graph.AddVertex('B');
            graph.AddVertex('C');
            graph.AddVertex('D');
            graph.AddVertex('E');

            graph.AddVertex('F');
            graph.AddVertex('G');
            graph.AddVertex('H');
            graph.AddVertex('I');

            graph.AddEdge('A', 'F');
            graph.AddEdge('B', 'F');
            graph.AddEdge('B', 'G');
            graph.AddEdge('C', 'H');
            graph.AddEdge('C', 'I');
            graph.AddEdge('D', 'G');
            graph.AddEdge('D', 'H');
            graph.AddEdge('E', 'F');
            graph.AddEdge('E', 'I');

            var algorithm = new DepthFirst<char>();

            HuaTuo.NUnit.Framework.Assert.IsTrue(algorithm.Find(graph, 'D'));

            HuaTuo.NUnit.Framework.Assert.IsFalse(algorithm.Find(graph, 'M'));

        }

    }
}
