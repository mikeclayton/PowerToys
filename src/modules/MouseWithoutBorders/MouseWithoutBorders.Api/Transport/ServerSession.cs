// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Sockets;

using Microsoft.Extensions.Logging;
using MouseWithoutBorders.Api.Transport.Events;

using Message = MouseWithoutBorders.Api.Models.Messages.Message;

#pragma warning disable CA1848 // Use the LoggerMethod delegates
namespace MouseWithoutBorders.Api.Transport;

public sealed class ServerSession : IDisposable
{
    public event EventHandler<MessageReceivedEventArgs> MessageReceived = (sender, e) => { };

    public ServerSession(ILogger logger, TcpClient tcpClient)
    {
        this.Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.TcpClient = tcpClient ?? throw new ArgumentNullException(nameof(tcpClient));
    }

    public ILogger Logger
    {
        get;
    }

    public TcpClient TcpClient
    {
        get;
    }

    private bool Disposed
    {
        get;
        set;
    }

    public void OnMessageReceived(MessageReceivedEventArgs e)
    {
        this.MessageReceived?.Invoke(this, e);
    }

    public void SendMessage(Message message)
    {
        this.SendMessage(message.CorrelationId, message.MessageType, message.MessageData);
    }

    public void SendMessage<T>(int correlationId, int messageType, T messageData)
        where T : struct
    {
        var payload = Message.Serialize(messageData);
        this.SendMessage(correlationId, messageType, payload);
    }

    public void SendMessage(int correlationId, int messageType, byte[]? messageData)
    {
        this.Logger.LogInformation($"session: {nameof(this.SendMessageAsync)}");

        var tcpClient = this.TcpClient ?? throw new InvalidOperationException();
        if (!tcpClient.Connected)
        {
            // client disconnected
            throw new InvalidOperationException("session: client disconnected");
        }

        var outboundStream = tcpClient.GetStream() ?? throw new InvalidOperationException();
        EndpointHelper.WriteMessage(outboundStream, correlationId, messageType, messageData);
    }

    public async Task SendMessageAsync(Message message, CancellationToken cancellationToken = default)
    {
        await this.SendMessageAsync(message.CorrelationId, message.MessageType, message.MessageData, cancellationToken);
    }

    public async Task SendMessageAsync(int correlationId, int messageType, CancellationToken cancellationToken = default)
    {
        await this.SendMessageAsync(correlationId, messageType, null, cancellationToken);
    }

    public async Task SendMessageAsync<T>(int correlationId, int messageType, T messageData, CancellationToken cancellationToken = default)
        where T : struct
    {
        var payload = Message.Serialize(messageData);
        await this.SendMessageAsync(correlationId, messageType, payload, cancellationToken);
    }

    public async Task SendMessageAsync(int correlationId, int messageType, byte[]? messageData, CancellationToken cancellationToken = default)
    {
        this.Logger.LogInformation($"session: {nameof(this.SendMessageAsync)}");

        var tcpClient = this.TcpClient ?? throw new InvalidOperationException();
        if (!tcpClient.Connected)
        {
            // client disconnected
            throw new InvalidOperationException("session: client disconnected");
        }

        var outboundStream = tcpClient.GetStream() ?? throw new InvalidOperationException();
        await EndpointHelper.WriteMessageAsync(outboundStream, correlationId, messageType, messageData, cancellationToken);
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
            this.TcpClient?.Dispose();
        }

        this.Disposed = true;
    }
}
