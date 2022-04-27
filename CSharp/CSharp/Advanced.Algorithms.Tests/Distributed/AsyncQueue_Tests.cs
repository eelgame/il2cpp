﻿using Advanced.Algorithms.Distributed;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Advanced.Algorithms.Tests.Distributed
{
    
    public class AsyncQueue_Tests
    {
        [HuaTuo.NUnit.Framework.Test]
        public void AsyncQueue_Test()
        {
            var queue = new AsyncQueue<int>();

            var testDataCount = 10000;

            var tasks = new List<Task>();

            var expected = new List<int>();

            var producerLock = new SemaphoreSlim(1);
            var consumerLock = new SemaphoreSlim(1);

            var random = new Random();

            //multi-threaded async producer
            tasks.AddRange(Enumerable.Range(1, testDataCount).Select(async x =>
            {
                await Task.Delay(random.Next(0, 1));

                await producerLock.WaitAsync();

                expected.Add(x);
                await queue.EnqueueAsync(x);

                producerLock.Release();
            }));

            var actual = new List<int>();

            //multi-threaded async consumer
            tasks.AddRange(Enumerable.Range(1, testDataCount).Select(async x =>
            {
                await Task.Delay(random.Next(0, 1));

                await consumerLock.WaitAsync();

                actual.Add(await queue.DequeueAsync());

                consumerLock.Release();
            }));

            Task.WaitAll(tasks.ToArray());

            HuaTuo.NUnit.Framework.Assert.AreEqual(expected, actual);
        }
    }
}
