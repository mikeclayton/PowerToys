// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace MouseWithoutBorders.Messaging;

internal sealed class PacketProducer
{
    public PacketProducer()
    {
        this.Queue = new();
    }

    public PacketQueue Queue
    {
        get;
    }

    public async ValueTask WriteAsync(DATA packet, CancellationToken cancellationToken = default)
    {
        await this.Queue.WriteAsync(packet, cancellationToken);
    }

    public bool TryWrite(DATA item)
    {
        return this.Queue.TryWrite(item);
    }
}
