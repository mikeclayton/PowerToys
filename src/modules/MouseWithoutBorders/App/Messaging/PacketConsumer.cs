// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace MouseWithoutBorders.Messaging;

internal sealed class PacketConsumer
{
    public PacketConsumer(Func<DATA, CancellationToken, Task> callback)
    {
        this.Channel = System.Threading.Channels.Channel.CreateBounded<DATA>(
            new BoundedChannelOptions(100)
            {
                SingleWriter = true,
                SingleReader = true,
                AllowSynchronousContinuations = true,
                FullMode = BoundedChannelFullMode.Wait,
            });
        this.Callback = callback ?? throw new ArgumentNullException(nameof(callback));
    }

    /// <remarks>
    /// Each PacketConsumer has a private channel to store its own copy of messages.
    /// When a message is posted to a PacketQueue it gets multiplexed to all the subscribing
    /// PacketConsumers.
    /// </remarks>
    private Channel<DATA> Channel
    {
        get;
    }

    private Func<DATA, CancellationToken, Task> Callback
    {
        get;
    }

    public int Count
        => this.Channel.Reader.Count;

    public async ValueTask WriteAsync(DATA packet, CancellationToken cancellationToken = default)
    {
        await this.Channel.Writer.WriteAsync(packet, cancellationToken);
    }

    public bool TryWrite(DATA packet)
    {
        return this.Channel.Writer.TryWrite(packet);
    }

    /// <remarks>
    /// See https://devblogs.microsoft.com/dotnet/an-introduction-to-system-threading-channels/
    /// </remarks>
    public async ValueTask StartAsync(CancellationToken cancellationToken = default)
    {
        var reader = this.Channel.Reader;
        while (true)
        {
            if (!await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
            {
                throw new ChannelClosedException();
            }

            if (reader.TryRead(out var packet))
            {
                await this.Callback(packet, cancellationToken);
            }
        }
    }

    /// <summary>
    /// Reads and processes all messages currently on the queue until it is empty.
    /// Any messages that arrive while draining will be read and processed as well.
    /// Does *not* "Complete" the queue, just leaves it empty.
    /// </summary>
    public async Task DrainAsync(CancellationToken cancellationToken = default)
    {
        while (this.Channel.Reader.Count > 0)
        {
            await Task.Delay(250, cancellationToken);
        }
    }

    public void Complete()
    {
        this.Channel.Writer.Complete();
    }
}
