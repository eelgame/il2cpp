﻿using Advanced.Algorithms.Graph;


namespace Advanced.Algorithms.Tests.Graph
{
    
    public class PushRelabel_Tests
    {
        /// <summary>
        /// PushRelabel Max Flow test
        /// </summary>
        [HuaTuo.NUnit.Framework.Test]
        public void PushRelabel_AdjacencyListGraph_Smoke_Test()
        {
            var graph = new Advanced.Algorithms.DataStructures.Graph.AdjacencyList.WeightedDiGraph<char, int>();

            graph.AddVertex('S');
            graph.AddVertex('A');
            graph.AddVertex('B');
            graph.AddVertex('C');
            graph.AddVertex('D');
            graph.AddVertex('T');

            graph.AddEdge('S', 'A', 10);
            graph.AddEdge('S', 'C', 10);

            graph.AddEdge('A', 'B', 4);
            graph.AddEdge('A', 'C', 2);
            graph.AddEdge('A', 'D', 8);

            graph.AddEdge('B', 'T', 10);

            graph.AddEdge('C', 'D', 9);

            graph.AddEdge('D', 'B', 6);
            graph.AddEdge('D', 'T', 10);

            var algorithm = new PushRelabelMaxFlow<char, int>(new PushRelabelOperators());

            var result = algorithm.ComputeMaxFlow(graph, 'S', 'T');

            HuaTuo.NUnit.Framework.Assert.AreEqual(result, 19);
        }
        /// <summary>
        /// PushRelabel Max Flow test
        /// </summary>
        [HuaTuo.NUnit.Framework.Test]
        public void PushRelabel_AdjacencyMatrixGraph_Smoke_Test()
        {
            var graph = new Advanced.Algorithms.DataStructures.Graph.AdjacencyMatrix.WeightedDiGraph<char, int>();

            graph.AddVertex('S');
            graph.AddVertex('A');
            graph.AddVertex('B');
            graph.AddVertex('C');
            graph.AddVertex('D');
            graph.AddVertex('T');

            graph.AddEdge('S', 'A', 10);
            graph.AddEdge('S', 'C', 10);

            graph.AddEdge('A', 'B', 4);
            graph.AddEdge('A', 'C', 2);
            graph.AddEdge('A', 'D', 8);

            graph.AddEdge('B', 'T', 10);

            graph.AddEdge('C', 'D', 9);

            graph.AddEdge('D', 'B', 6);
            graph.AddEdge('D', 'T', 10);

            var algorithm = new PushRelabelMaxFlow<char, int>(new PushRelabelOperators());

            var result = algorithm.ComputeMaxFlow(graph, 'S', 'T');

            HuaTuo.NUnit.Framework.Assert.AreEqual(result, 19);
        }

        /// <summary>
        /// operators for generics
        /// implemented for int type for edge weights
        /// </summary>
        public class PushRelabelOperators : IFlowOperators<int>
        {
            public int AddWeights(int a, int b)
            {
                return checked(a + b);
            }

            public int defaultWeight
            {
                get
                {
                    return 0;
                }
            }

            public int MaxWeight
            {
                get
                {
                    return int.MaxValue;
                }
            }

            public int SubstractWeights(int a, int b)
            {
                return checked(a - b);
            }
        }
    }
}
