// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Threading;

using MouseWithoutBorders.Api.Models;
using MouseWithoutBorders.Api.Models.Messages;

namespace MouseWithoutBorders.Api.Transport.Events;

public sealed class MessageReceivedEventArgs : EventArgs
{
    public MessageReceivedEventArgs(Message message, CancellationToken cancellationToken)
    {
        this.Message = message ?? throw new ArgumentNullException(nameof(message));
        this.CancellationToken = cancellationToken;
    }

    public Message Message
    {
        get;
    }

    public CancellationToken CancellationToken
    {
        get;
    }
}
