// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MouseWithoutBorders.Messaging;

internal sealed class PacketQueue
{
    public PacketQueue()
    {
        this.Consumers = [];
    }

    private Lock _consumerLock = new();

    private ConcurrentBag<PacketConsumer> Consumers
    {
        get;
        set;
    }

    public void Subscribe(PacketConsumer consumer)
    {
        ArgumentNullException.ThrowIfNull(consumer);

        // we still need to lock because Unsubscribe replaces the instance
        // so there's a race condition where Subscribe could be called to
        // add a new consumer half way though Unsubscribe already running
        // and the new consumer getting lost from the new value.
        lock (this._consumerLock)
        {
            this.Consumers.Add(consumer);
        }
    }

    public void Unsubscribe(PacketConsumer consumer)
    {
        ArgumentNullException.ThrowIfNull(consumer);

        // we still need to lock because Unsubscribe replaces the instance
        // so there's a race condition where Subscribe could be called to
        // add a new consumer half way though Unsubscribe already running
        // and the new consumer getting lost from the new value.
        lock (this._consumerLock)
        {
            this.Consumers = new(
                this.Consumers.Where(
                    entry => !object.ReferenceEquals(entry, consumer)));
        }
    }

    public async ValueTask WriteAsync(DATA packet, CancellationToken cancellationToken = default)
    {
        // we don't need to lock while enumerating because we don't care too much
        // if a single message gets lost while a new consumer is being added
        foreach (var consumer in this.Consumers)
        {
            await consumer.WriteAsync(packet, cancellationToken);
        }
    }

    public bool TryWrite(DATA packet)
    {
        // we don't need to lock while enumerating because we don't care too much
        // if a single message gets lost while a new consumer is being added
        var result = true;
        foreach (var consumer in this.Consumers)
        {
            result &= consumer.TryWrite(packet);
        }

        return result;
    }
}
