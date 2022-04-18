using Advanced.Algorithms.Graph;


namespace Advanced.Algorithms.Tests.Graph
{

    
    public class TarjansBiConnected_Tests
    {
        [NUnit.Framework.Test]
        public void TarjanIsBiConnected_AdjacencyListGraph_Smoke_Test()
        {
            var graph = new Advanced.Algorithms.DataStructures.Graph.AdjacencyList.Graph<char>();

            graph.AddVertex('A');
            graph.AddVertex('B');
            graph.AddVertex('C');

            graph.AddEdge('A', 'B');
            graph.AddEdge('A', 'C');
            graph.AddEdge('B', 'C');

            var algorithm = new TarjansBiConnected<char>();

            var result = algorithm.IsBiConnected(graph);

            NUnit.Framework.Assert.IsTrue(result);

            graph.AddVertex('D');
            graph.AddVertex('E');
            graph.AddVertex('F');
            graph.AddVertex('G');
            graph.AddVertex('H');

            graph.AddEdge('C', 'D');
            graph.AddEdge('D', 'E');

            graph.AddEdge('E', 'F');
            graph.AddEdge('F', 'G');
            graph.AddEdge('G', 'E');

            graph.AddEdge('F', 'H');

            result = algorithm.IsBiConnected(graph);

            NUnit.Framework.Assert.IsFalse(result);

        }

        [NUnit.Framework.Test]
        public void TarjanIsBiConnected_AdjacencyMatrixGraph_Smoke_Test()
        {
            var graph = new Advanced.Algorithms.DataStructures.Graph.AdjacencyMatrix.Graph<char>();

            graph.AddVertex('A');
            graph.AddVertex('B');
            graph.AddVertex('C');

            graph.AddEdge('A', 'B');
            graph.AddEdge('A', 'C');
            graph.AddEdge('B', 'C');

            var algorithm = new TarjansBiConnected<char>();

            var result = algorithm.IsBiConnected(graph);

            NUnit.Framework.Assert.IsTrue(result);

            graph.AddVertex('D');
            graph.AddVertex('E');
            graph.AddVertex('F');
            graph.AddVertex('G');
            graph.AddVertex('H');

            graph.AddEdge('C', 'D');
            graph.AddEdge('D', 'E');

            graph.AddEdge('E', 'F');
            graph.AddEdge('F', 'G');
            graph.AddEdge('G', 'E');

            graph.AddEdge('F', 'H');

            result = algorithm.IsBiConnected(graph);

            NUnit.Framework.Assert.IsFalse(result);

        }
    }
}
