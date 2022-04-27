using Advanced.Algorithms.Graph;

namespace Advanced.Algorithms.Tests.Graph
{
    
    public class BreadFirst_Tests
    {
        [HuaTuo.NUnit.Framework.Test]
        public void BreadthFirst_AdjacencyListGraph_Smoke_Test()
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

            graph.AddEdge('A', 'B');
            graph.AddEdge('B', 'C');
            graph.AddEdge('C', 'D');
            graph.AddEdge('D', 'E');
            graph.AddEdge('E', 'F');
            graph.AddEdge('F', 'G');
            graph.AddEdge('G', 'H');
            graph.AddEdge('H', 'I');

            var algorithm = new BreadthFirst<char>();

            HuaTuo.NUnit.Framework.Assert.IsTrue(algorithm.Find(graph, 'D'));

            HuaTuo.NUnit.Framework.Assert.IsFalse(algorithm.Find(graph, 'M'));

        }

        [HuaTuo.NUnit.Framework.Test]
        public void BreadthFirst_AdjacencyMatrixGraph_Smoke_Test()
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

            graph.AddEdge('A', 'B');
            graph.AddEdge('B', 'C');
            graph.AddEdge('C', 'D');
            graph.AddEdge('D', 'E');
            graph.AddEdge('E', 'F');
            graph.AddEdge('F', 'G');
            graph.AddEdge('G', 'H');
            graph.AddEdge('H', 'I');

            var algorithm = new BreadthFirst<char>();

            HuaTuo.NUnit.Framework.Assert.IsTrue(algorithm.Find(graph, 'D'));

            HuaTuo.NUnit.Framework.Assert.IsFalse(algorithm.Find(graph, 'M'));

        }
    }
}
