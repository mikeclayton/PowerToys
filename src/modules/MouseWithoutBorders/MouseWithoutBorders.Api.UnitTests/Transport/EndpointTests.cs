// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Net;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MouseWithoutBorders.Api.Models.Messages;
using MouseWithoutBorders.Api.Transport;

namespace MouseWithoutBorders.Api.UnitTests.Core;

[TestClass]
public class EndPointTests
{
    /// <summary>
    /// Spins up a local api server and client, then pumps a collection of dummy messages
    /// from the client to the server. The server echoes the messages back to the client
    /// as responses, and a separate consumer task reads the messages from the client buffer.
    /// </summary>
    [TestMethod]
    public async Task BasicSmokeTest()
    {
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
        });
        var logger = loggerFactory.CreateLogger<Message>();

        var cancellationTokenSource = new CancellationTokenSource();

        var stopwatch = Stopwatch.StartNew();

        // start the server
        var serverCount = 0;
        var server = new ServerEndpoint(
            logger: logger,
            name: "server",
            address: IPAddress.Loopback,
            port: 12345,
            callback: async (server, session, message, cancellationToken) =>
            {
                // echo messages back to the client
                await Task.Run(
                    async () =>
                    {
                        Interlocked.Increment(ref serverCount);
                        await session
                            .SendMessageAsync(message, cancellationToken)
                            .ConfigureAwait(false);
                    },
                    cancellationToken);
            });

        var serverTask = Task.Run(
            () => server
                .StartServerAsync(cancellationTokenSource.Token)
                .ConfigureAwait(false));

        // start the client
        using var localClient = new ClientEndpoint(
            logger: logger,
            name: "client",
            serverAddress: IPAddress.Loopback,
            serverPort: 12345);
        await localClient.ConnectAsync();

        // start a consumer that will drain the client buffer
        var clientCount = 0;
        var consumerTask = Task.Run(async () =>
        {
            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                var message = await localClient
                    .ReadMessageAsync(cancellationTokenSource.Token)
                    .ConfigureAwait(false);
                Interlocked.Increment(ref clientCount);
            }
        });

        // push test messages onto the client
        var messageCount = 250_000;
        for (var i = 0; i < messageCount; i++)
        {
            await localClient
                .SendMessageAsync(new Message(i, 1))
                .ConfigureAwait(false);
        }

        // wait for all messages to be roundtripped and consumed
        while (serverCount < messageCount)
        {
            var timeoutTask = Task.Delay(250);
            var completedTask = await Task.WhenAny(
                Task.WhenAll(serverTask, consumerTask),
                timeoutTask);
            if (completedTask != timeoutTask)
            {
                break;
            }
        }

        stopwatch.Stop();

        // make sure we didn't take too long.
        // 250,000 messages roundtrip in about 20 seconds on my machine (= 12,500/s),
        // so lets give it a little bit longer to avoid lots of test failures
        Assert.IsTrue(stopwatch.Elapsed < TimeSpan.FromSeconds(25), "Test took too long!");

        // make sure we didn't drop any messages
        Assert.AreEqual(messageCount, serverCount);
        Assert.AreEqual(messageCount, clientCount);
    }
}
