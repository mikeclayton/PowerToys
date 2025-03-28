// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Threading;

namespace MouseWithoutBorders.Api.Transport.Events;

public sealed class ClientConnectedEventArgs : EventArgs
{
    public ClientConnectedEventArgs(ServerSession serverSession, CancellationToken cancellationToken)
    {
        this.ServerSession = serverSession ?? throw new ArgumentNullException(nameof(serverSession));
        this.CancellationToken = cancellationToken;
    }

    public ServerSession ServerSession
    {
        get;
    }

    public CancellationToken CancellationToken
    {
        get;
    }
}
