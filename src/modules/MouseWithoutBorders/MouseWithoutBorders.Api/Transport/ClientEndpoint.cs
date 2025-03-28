// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Net.Sockets;

using Microsoft.Extensions.Logging;
using MouseWithoutBorders.Api.Transport.Events;

using Message = MouseWithoutBorders.Api.Models.Messages.Message;

#pragma warning disable CA1848 // Use the LoggerMethod delegates
namespace MouseWithoutBorders.Api.Transport;

public sealed class ClientEndpoint : IDisposable
{
    public event EventHandler<MessageReceivedEventArgs> MessageReceived = (sender, e) => { };

    public ClientEndpoint(ILogger logger, string name, IPAddress serverAddress, int serverPort)
    {
        this.Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.Name = name ?? throw new ArgumentNullException(nameof(name));
        this.ServerAddress = serverAddress ?? throw new ArgumentNullException(nameof(serverAddress));
        this.ServerPort = serverPort;
    }

    ~ClientEndpoint()
    {
        this.Dispose(false);
    }

    private ILogger Logger
    {
        get;
    }

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

    private bool Disposed
    {
        get;
        set;
    }

    public void OnMessageReceived(MessageReceivedEventArgs e)
    {
        this.MessageReceived?.Invoke(this, e);
    }

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        this.TcpClient = new TcpClient();

        // open a connection to the server
        this.Logger.LogInformation("client: connecting to server");
        await this.TcpClient.ConnectAsync(this.ServerAddress, this.ServerPort, cancellationToken);
        this.Logger.LogInformation("client: connected to server");

        // listen for messages coming back from the server
        this.Logger.LogInformation("client: starting listener");
        var inboundStream = this.TcpClient.GetStream();
        _ = Task.Run(() => this.ReceiveMessagesAsync(inboundStream, cancellationToken), cancellationToken);
        this.Logger.LogInformation("client: listener started...");
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
        this.Logger.LogInformation($"client: {nameof(this.SendMessageAsync)}");

        var tcpClient = this.TcpClient ?? throw new InvalidOperationException();
        if (!tcpClient.Connected)
        {
            // server disconnected
            throw new InvalidOperationException("client: server disconnected");
        }

        var outboundStream = tcpClient.GetStream() ?? throw new InvalidOperationException();
        await EndpointHelper.WriteMessageAsync(outboundStream, correlationId, messageType, messageData, cancellationToken);
    }

    private async Task ReceiveMessagesAsync(Stream inboundStream, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await this.ReceiveMessageAsync(inboundStream, cancellationToken);
        }
    }

    private async Task ReceiveMessageAsync(Stream inboundStream, CancellationToken cancellationToken)
    {
        var message = await EndpointHelper.ReadMessageAsync(inboundStream, cancellationToken);
        if (message == null)
        {
            return;
        }

        this.OnMessageReceived(new MessageReceivedEventArgs(message, cancellationToken));
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
