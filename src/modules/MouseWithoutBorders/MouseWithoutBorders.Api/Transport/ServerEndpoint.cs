// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Net.Sockets;

using Microsoft.Extensions.Logging;
using MouseWithoutBorders.Api.Transport.Events;

#pragma warning disable CA1848 // Use the LoggerMethod delegates
namespace MouseWithoutBorders.Api.Transport;

public sealed class ServerEndpoint : IDisposable
{
    public event EventHandler<ClientConnectedEventArgs> ClientConnected = (sender, e) => { };

    public ServerEndpoint(ILogger logger, string name, IPAddress address, int port)
    {
        this.Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.Name = name ?? throw new ArgumentNullException(nameof(name));
        this.Address = address ?? throw new ArgumentNullException(nameof(address));
        this.Port = port;
    }

    ~ServerEndpoint()
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

    public IPAddress Address
    {
        get;
    }

    public int Port
    {
        get;
    }

    private TcpClient? TcpClient
    {
        get;
        set;
    }

    private Stream? OutboundStream
    {
        get;
        set;
    }

    private bool Disposed
    {
        get;
        set;
    }

    public void OnClientConnected(ClientConnectedEventArgs e)
    {
        this.ClientConnected?.Invoke(this, e);
    }

    public async Task StartServerAsync(CancellationToken cancellationToken = default)
    {
        var listener = new TcpListener(this.Address, this.Port);
        listener.Start();
        this.Logger.LogInformation("server: listener started...");

        while (!cancellationToken.IsCancellationRequested)
        {
            var client = await listener.AcceptTcpClientAsync(cancellationToken);
            var remoteEndpoint = client.Client.RemoteEndPoint as IPEndPoint
                ?? throw new InvalidOperationException();
            this.Logger.LogInformation(
                "server: client connection accepted from '{RemoteEndpointAddress}:{RemoteEndpointPort}'",
                remoteEndpoint.Address,
                remoteEndpoint.Port);

            _ = Task.Run(() => this.HandleClientAsync(client, cancellationToken), cancellationToken);
        }
    }

    private async Task HandleClientAsync(TcpClient tcpClient, CancellationToken cancellationToken = default)
    {
        this.Logger.LogInformation($"server: {nameof(HandleClientAsync)}");
        var serverSession = new ServerSession(this.Logger, tcpClient);
        this.OnClientConnected(new ClientConnectedEventArgs(serverSession, cancellationToken));
        await ServerEndpoint.ReceiveMessagesAsync(serverSession, cancellationToken);
    }

    private static async Task ReceiveMessagesAsync(ServerSession serverSession, CancellationToken cancellationToken)
     {
        while (!cancellationToken.IsCancellationRequested)
        {
            await ServerEndpoint.ReceiveMessageAsync(serverSession, cancellationToken);
        }
    }

    private static async Task ReceiveMessageAsync(ServerSession serverSession, CancellationToken cancellationToken)
    {
        var inboundStream = serverSession.TcpClient.GetStream();
        var message = await EndpointHelper.ReadMessageAsync(inboundStream, cancellationToken);
        if (message == null)
        {
            return;
        }

        serverSession.OnMessageReceived(new MessageReceivedEventArgs(message, cancellationToken));
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
            this.OutboundStream?.Dispose();
        }

        this.Disposed = true;
    }
}
