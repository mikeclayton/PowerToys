// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MouseWithoutBorders.Messaging;

namespace MouseWithoutBorders.UnitTests.Messaging;

public static class PacketProducerTests
{
    [TestClass]
    public sealed class GeneralTests
    {
        /// <summary>
        /// Performs a basic smoke and performance test, and ensures that the same number of messages
        /// posted to a PacketQueue by a PacketProducer are received and processed by multiple PacketConsumers.
        /// </summary>
        [TestMethod]
        public async Task BasicSmokeAndPerformanceTest()
        {
            // some bookkeeping for the test itself
            var messageCount = 1_000_000;
            var triggers = new ConcurrentDictionary<string, int>();

            // make a producer that we'll use to push messages onto a queue
            var producer = new PacketProducer();

            // subscribe a first consumer to the producer's queue and start it.
            // when invoked, it just updates how many times it's been called so we can make sure no messages get missed
            PacketConsumer consumer1 = new(
                (DATA packet, CancellationToken cancellationToken) =>
                {
                    triggers.AddOrUpdate(nameof(consumer1), 1, (key, oldValue) => oldValue + 1);
                    return Task.CompletedTask;
                });
            producer.Queue.Subscribe(consumer1);
            var task1 = Task.Run(() => consumer1.StartAsync());

            // subscribe a second consumer to the producer's queue and start it.
            // when invoked, it just updates how many times it's been called so we can make sure no messages get missed
            PacketConsumer consumer2 = new(
                (DATA packet, CancellationToken cancellationToken) =>
                {
                    triggers.AddOrUpdate(nameof(consumer2), 1, (key, oldValue) => oldValue + 1);
                    return Task.CompletedTask;
                });
            producer.Queue.Subscribe(consumer2);
            var task2 = Task.Run(() => consumer2.StartAsync());

            // post a bunch of messages onto the queue
            var stopwatch = Stopwatch.StartNew();
            for (var i = 0; i < messageCount; i++)
            {
                await producer.WriteAsync(new());
            }

            // wait for all the messages to be processed by both consumers
            await Task.WhenAll(
                consumer1.DrainAsync(),
                consumer2.DrainAsync());

            // check how long it took to process the messages
            // this should typically only be a few thousand milliseconds for about 1,000,000 messages
            stopwatch.Stop();
            Console.WriteLine($"{messageCount:N0} messages processed in {stopwatch.ElapsedMilliseconds}ms");

            // did we miss any messages?
            Assert.IsTrue(triggers.ContainsKey(nameof(consumer1)));
            Assert.AreEqual(messageCount, triggers[nameof(consumer1)]);
            Assert.IsTrue(triggers.ContainsKey(nameof(consumer2)));
            Assert.AreEqual(messageCount, triggers[nameof(consumer2)]);

            // the test will ideally a *little* bit quicker than this, but we'll set it as
            // an upper limit so we don't get lots of false negatives.
            var performanceGoal = 4000; // milliseconds
            Assert.IsTrue(
                stopwatch.ElapsedMilliseconds <= performanceGoal,
                $"Time taken was expected to be {performanceGoal}ms or less, but was {stopwatch.ElapsedMilliseconds}ms.");
        }
    }
}
