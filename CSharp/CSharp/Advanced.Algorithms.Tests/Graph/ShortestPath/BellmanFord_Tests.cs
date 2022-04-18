﻿using Advanced.Algorithms.DataStructures.Graph.AdjacencyList;
using Advanced.Algorithms.Graph;



namespace Advanced.Algorithms.Tests.Graph
{
    
    public class BellmanFord_Tests
    {
        [NUnit.Framework.Test]
        public void BellmanFord_Smoke_Test()
        {
            var graph = new WeightedDiGraph<char, int>();

            graph.AddVertex('S');
            graph.AddVertex('A');
            graph.AddVertex('B');
            graph.AddVertex('C');
            graph.AddVertex('D');
            graph.AddVertex('T');

            graph.AddEdge('S', 'A', -10);
            graph.AddEdge('S', 'C', -5);

            graph.AddEdge('A', 'B', 4);
            graph.AddEdge('A', 'C', 2);
            graph.AddEdge('A', 'D', 8);

            graph.AddEdge('B', 'T', 10);

            graph.AddEdge('C', 'D', 9);

            graph.AddEdge('D', 'B', 6);
            graph.AddEdge('D', 'T', 10);

            var algorithm = new BellmanFordShortestPath<char, int>(new BellmanFordShortestPathOperators());

            var result = algorithm.FindShortestPath(graph, 'S', 'T');

            NUnit.Framework.Assert.AreEqual(4, result.Length);

            var expectedPath = new char[] { 'S', 'A', 'B', 'T' };
            for (int i = 0; i < expectedPath.Length; i++)
            {
                NUnit.Framework.Assert.AreEqual(expectedPath[i], result.Path[i]);
            }

        }

        /// <summary>
        /// generic operations for int type
        /// </summary>
        public class BellmanFordShortestPathOperators : IShortestPathOperators<int>
        {
            public int DefaultValue
            {
                get
                {
                    return 0;
                }
            }

            public int MaxValue
            {
                get
                {
                    return int.MaxValue;
                }
            }

            public int Sum(int a, int b)
            {
                return checked(a + b);
            }
        }
    }
}
