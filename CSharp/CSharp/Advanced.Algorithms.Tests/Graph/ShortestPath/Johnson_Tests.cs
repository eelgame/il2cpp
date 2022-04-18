﻿using Advanced.Algorithms.Graph;

using System.Linq;

namespace Advanced.Algorithms.Tests.Graph
{
    
    public class Johnsons_Tests
    {
        [NUnit.Framework.Test]
        public void Johnsons_AdjacencyListGraph_Smoke_Test()
        {
            var graph = new Advanced.Algorithms.DataStructures.Graph.AdjacencyList.WeightedDiGraph<char, int>();

            graph.AddVertex('S');
            graph.AddVertex('A');
            graph.AddVertex('B');
            graph.AddVertex('C');
            graph.AddVertex('D');
            graph.AddVertex('T');

            graph.AddEdge('S', 'A', -5);
            graph.AddEdge('S', 'C', 10);

            graph.AddEdge('A', 'B', 10);
            graph.AddEdge('A', 'C', 1);
            graph.AddEdge('A', 'D', 8);

            graph.AddEdge('B', 'T', 4);

            graph.AddEdge('C', 'D', 1);

            graph.AddEdge('D', 'B', 1);
            graph.AddEdge('D', 'T', 10);

            var algorithm = new JohnsonsShortestPath<char, int>(new JohnsonsShortestPathOperators());

            var result = algorithm.FindAllPairShortestPaths(graph);

            var testCase = result.First(x => x.Source == 'S' && x.Destination == 'T');
            NUnit.Framework.Assert.AreEqual(2, testCase.Distance);

            var expectedPath = new char[] { 'S', 'A', 'C', 'D', 'B', 'T' };
            for (int i = 0; i < expectedPath.Length; i++)
            {
                NUnit.Framework.Assert.AreEqual(expectedPath[i], testCase.Path[i]);
            }

        }


        [NUnit.Framework.Test]
        public void Johnsons_AdjacencyMatrixGraph_Smoke_Test()
        {
            var graph = new Advanced.Algorithms.DataStructures.Graph.AdjacencyMatrix.WeightedDiGraph<char, int>();

            graph.AddVertex('S');
            graph.AddVertex('A');
            graph.AddVertex('B');
            graph.AddVertex('C');
            graph.AddVertex('D');
            graph.AddVertex('T');

            graph.AddEdge('S', 'A', -5);
            graph.AddEdge('S', 'C', 10);

            graph.AddEdge('A', 'B', 10);
            graph.AddEdge('A', 'C', 1);
            graph.AddEdge('A', 'D', 8);

            graph.AddEdge('B', 'T', 4);

            graph.AddEdge('C', 'D', 1);

            graph.AddEdge('D', 'B', 1);
            graph.AddEdge('D', 'T', 10);

            var algorithm = new JohnsonsShortestPath<char, int>(new JohnsonsShortestPathOperators());

            var result = algorithm.FindAllPairShortestPaths(graph);

            var testCase = result.First(x => x.Source == 'S' && x.Destination == 'T');
            NUnit.Framework.Assert.AreEqual(2, testCase.Distance);

            var expectedPath = new char[] { 'S', 'A', 'C', 'D', 'B', 'T' };
            for (int i = 0; i < expectedPath.Length; i++)
            {
                NUnit.Framework.Assert.AreEqual(expectedPath[i], testCase.Path[i]);
            }

        }
        /// <summary>
        /// generic operations for int type
        /// </summary>
        public class JohnsonsShortestPathOperators
            : IJohnsonsShortestPathOperators<char, int>
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

            /// <summary>
            /// return a char not in graph
            /// </summary>
            /// <returns></returns>
            public char RandomVertex()
            {
                return '*';
            }

            public int Substract(int a, int b)
            {
                return checked(a - b);
            }

            public int Sum(int a, int b)
            {
                return checked(a + b);
            }
        }
    }
}
