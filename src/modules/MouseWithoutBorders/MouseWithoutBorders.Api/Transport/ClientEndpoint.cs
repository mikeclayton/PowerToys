﻿// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;

using Message = MouseWithoutBorders.Api.Models.Messages.Message;

#pragma warning disable CA1848 // Use the LoggerMethod delegates
namespace MouseWithoutBorders.Api.Transport;

/// <summary>
/// Represents a client endpoint that can send and receive structured binary messages
/// to and from a server. The client endpoint is responsible for managing a TCP/IP
/// network connection to the specified server address and port, and for sending and
/// receiving messages over that connection. Internally, the client endpoint uses a
/// local channel to queue messages to be sent to the server and a channel to buffer
/// messages received from the server.
/// </summary>
public sealed class ClientEndpoint : IDisposable
{
    public ClientEndpoint(ILogger logger, string name, IPAddress serverAddress, int serverPort)
    {
        this.Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.Name = name ?? throw new ArgumentNullException(nameof(name));
        this.ServerAddress = serverAddress ?? throw new ArgumentNullException(nameof(serverAddress));
        this.ServerPort = serverPort;
        this.SendBuffer = Channel.CreateBounded<Message>(250);
        this.ReceiveBuffer = Channel.CreateBounded<Message>(250);
        this.StopTokenSource = new CancellationTokenSource();
    }

    ~ClientEndpoint()
    {
        this.Dispose(false);
    }

    private ILogger Logger
    {
        get;
    }

    /// <summary>
    /// Gets a name for the client endpoint that can be used to identify
    /// it in log messages and other diagnostic output.
    /// </summary>
    public string Name
    {
        get;
    }

    public IPAddress ServerAddress
    {
        get;
    }

    public int ServerPort
    {
        get;
    }

    private TcpClient? TcpClient
    {
        get;
        set;
    }

    /// <summary>
    /// Gets the channel used to buffer messages internally while they wait to be sent to the server.
    /// </summary>
    private Channel<Message> SendBuffer
    {
        get;
    }

    /// <summary>
    /// Gets the channel used to buffer messages internally when they are received from the server.
    /// </summary>
    private Channel<Message> ReceiveBuffer
    {
        get;
    }

    private CancellationTokenSource StopTokenSource
    {
        get;
    }

    private bool Disposed
    {
        get;
        set;
    }

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        // open a connection to the server
        this.Logger.LogInformation("client: connecting to server");
        this.TcpClient = new TcpClient();
        await this.TcpClient.ConnectAsync(this.ServerAddress, this.ServerPort, cancellationToken);
        this.Logger.LogInformation("client: connected to server");

        // create a combined cancellation token so the caller can stop the client,
        // or we can stop it without having to cancel the caller's token
        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            this.StopTokenSource.Token, cancellationToken);

        // listen for messages coming back from the server
        // (start this first so we don't miss any response messages)
        this.Logger.LogInformation("client: starting network reader");
        _ = Task.Run(() => EndpointHelper.StartNetworkReceiverAsync(this.TcpClient, this.ReceiveBuffer, linkedCts.Token), cancellationToken);
        this.Logger.LogInformation("client: network reader started...");

        // pump messages from the client's "send" buffer up to the server
        this.Logger.LogInformation("client: starting network writer");
        _ = Task.Run(() => EndpointHelper.StartNetworkSenderAsync(this.SendBuffer, this.TcpClient, linkedCts.Token), cancellationToken);
        this.Logger.LogInformation("client: network writer started...");
    }

    /// <summary>
    /// Sends a message to the server.
    /// </summary>
    public async Task SendMessageAsync(Message message, CancellationToken cancellationToken = default)
    {
        await this.SendBuffer.Writer.WriteAsync(message, cancellationToken);
    }

    public async Task<Message> ReadMessageAsync(CancellationToken cancellationToken)
    {
        // create a combined cancellation token so the caller can stop the client,
        // or we can stop it without having to cancel the caller's token
        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            this.StopTokenSource.Token, cancellationToken);

        var reader = this.ReceiveBuffer.Reader;
        var message = await reader.ReadAsync(linkedCts.Token);
        return message;
    }

    public async Task<Message> WaitForMessageAsync(Func<Message, bool> predicate, CancellationToken cancellationToken)
    {
        // create a combined cancellation token so the caller can stop the client,
        // or we can stop it without having to cancel the caller's token
        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            this.StopTokenSource.Token, cancellationToken);

        var reader = this.ReceiveBuffer.Reader;
        while (!linkedCts.IsCancellationRequested)
        {
            var message = await reader.ReadAsync(linkedCts.Token);
            if (predicate(message))
            {
                return message;
            }
        }

        throw new OperationCanceledException();
    }

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (this.Disposed)
        {
            return;
        }

        if (disposing)
        {
            this.StopTokenSource.Cancel();
            this.TcpClient?.Dispose();
        }

        this.Disposed = true;
    }
}
