// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Net;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MouseWithoutBorders.Api.Models.Messages;
using MouseWithoutBorders.Api.Transport;

namespace MouseWithoutBorders.Api.UnitTests.Transport;

[TestClass]
public class EndPointTests
{
    /// <summary>
    /// Spins up a loopback api server and client, then pumps a collection of dummy messages
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

        // build the server
        var serverCount = 0;
        var serverEndpoint = new ServerEndpoint(
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

        // start the server
        var serverTask = Task.Run(
            () => serverEndpoint
                .StartServerAsync(cancellationTokenSource.Token)
                .ConfigureAwait(false));

        // build the client
        using var clientEndpoint = new ClientEndpoint(
            logger: logger,
            name: "client",
            serverAddress: IPAddress.Loopback,
            serverPort: 12345);

        // start the client
        await clientEndpoint.ConnectAsync();

        // start a consumer that will drain the client buffer
        // (this simulates an application reading the responses)
        var consumerCount = 0;
        var consumerTask = Task.Run(async () =>
        {
            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                var message = await clientEndpoint
                    .ReadMessageAsync(cancellationTokenSource.Token)
                    .ConfigureAwait(false);
                Interlocked.Increment(ref consumerCount);
            }
        });

        // push a bunch of test messages onto the client
        var messageCount = 250_000;
        for (var i = 0; i < messageCount; i++)
        {
            await clientEndpoint
                .SendMessageAsync(new Message(i, (int)MessageType.HeartbeatMessage))
                .ConfigureAwait(false);
        }

        // make sure we don't take too long to process all the messages.
        // 250,000 messages take about 10 seconds to roundtrip on my machine (~25,000/s),
        // so lets give it a little bit longer to avoid lots of test failures
        var timeoutTask = Task.Delay(TimeSpan.FromSeconds(12.5));

        // run a sleep loop to wait for all messages to be roundtripped and consumed
        // (or until we hit our timeout, so that we don't run forever)
        while ((serverCount < messageCount) || (consumerCount < messageCount))
        {
            var completedTask = await Task.WhenAny(timeoutTask, Task.Delay(250)).ConfigureAwait(false);
            if (completedTask == timeoutTask)
            {
                Assert.Fail("Test took too long!");
            }
        }

        stopwatch.Stop();

        // make sure we processed all messages
        Assert.AreEqual(messageCount, serverCount);
        Assert.AreEqual(messageCount, consumerCount);
    }
}
