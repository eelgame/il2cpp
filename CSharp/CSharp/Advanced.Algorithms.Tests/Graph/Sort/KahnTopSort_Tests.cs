using Advanced.Algorithms.Graph;



namespace Advanced.Algorithms.Tests.Graph
{
    
    public class KahnsTopSort_Tests
    {
        [NUnit.Framework.Test]
        public void Kahns_Topological_Sort_AdjancencyListGraph_Smoke_Test()
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

            graph.AddEdge('C', 'D');
            graph.AddEdge('E', 'D');

            graph.AddEdge('E', 'F');
            graph.AddEdge('F', 'G');

            graph.AddEdge('F', 'H');

            var algorithm = new KahnsTopSort<char>();

            var result = algorithm.GetTopSort(graph);

            NUnit.Framework.Assert.AreEqual(result.Count, 8);
        }

        [NUnit.Framework.Test]
        public void Kahns_Topological_Sort_AdjancencyMatrixGraph_Smoke_Test()
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

            graph.AddEdge('C', 'D');
            graph.AddEdge('E', 'D');

            graph.AddEdge('E', 'F');
            graph.AddEdge('F', 'G');

            graph.AddEdge('F', 'H');

            var algorithm = new KahnsTopSort<char>();

            var result = algorithm.GetTopSort(graph);

            NUnit.Framework.Assert.AreEqual(result.Count, 8);
        }
    }
}
