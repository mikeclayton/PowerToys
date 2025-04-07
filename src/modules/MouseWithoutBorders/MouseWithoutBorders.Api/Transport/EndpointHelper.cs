// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;

using Message = MouseWithoutBorders.Api.Models.Messages.Message;

namespace MouseWithoutBorders.Api.Transport;

internal static class EndpointHelper
{
    /// <summary>
    /// Starts the network sender.
    /// Reads messages from the "sender" channel and writes them to the network stream.
    /// </summary>
    public static async Task StartNetworkSenderAsync(Channel<Message> sendBuffer, TcpClient outboundClient, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(outboundClient);
        if (!outboundClient.Connected)
        {
            // server disconnected
            throw new InvalidOperationException("tcp client is disconnected");
        }

        var channelReader = sendBuffer.Reader;
        var outboundStream = outboundClient.GetStream();
        while (!cancellationToken.IsCancellationRequested)
        {
            // read a message from the "sender" channel
            var message = await channelReader.ReadAsync(cancellationToken).ConfigureAwait(false);

            // write the message to the network stream for the server to pick up
            await EndpointHelper.WriteMessageAsync(outboundStream, message, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Starts the network receiver.
    /// Reads messages from the network stream and writes them to the "receive" channel.
    /// </summary>
    public static async Task StartNetworkReceiverAsync(TcpClient inboundClient, Channel<Message> receiveBuffer, CancellationToken cancellationToken)
    {
        // create a combined cancellation token so the caller can stop the client,
        // or we can stop it without having to cancel the caller's token
        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, cancellationToken);

        var inboundStream = (inboundClient ?? throw new InvalidOperationException()).GetStream();
        var channelWriter = receiveBuffer.Writer;
        while (!linkedCts.IsCancellationRequested)
        {
            // read a message from the network stream
            var message = await EndpointHelper.ReadMessageAsync(inboundStream, linkedCts.Token).ConfigureAwait(false);

            // write the message to the "receive" channel for the caller to pick up
            await channelWriter.WriteAsync(message, linkedCts.Token).ConfigureAwait(false);
        }
    }

    public static async Task<Message> ReadMessageAsync(Stream inboundStream, CancellationToken cancellationToken)
    {
        // read the correlation id
        var correlationIdBuffer = new byte[4];
        await EndpointHelper.FillBufferAsync(inboundStream, correlationIdBuffer, cancellationToken).ConfigureAwait(false);
        var correlationId = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(correlationIdBuffer));

        // read the message type
        var messageTypeBuffer = new byte[4];
        await EndpointHelper.FillBufferAsync(inboundStream, messageTypeBuffer, cancellationToken).ConfigureAwait(false);
        var messageType = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(messageTypeBuffer));

        // read the data length
        var messageDataLengthBuffer = new byte[4];
        await EndpointHelper.FillBufferAsync(inboundStream, messageDataLengthBuffer, cancellationToken).ConfigureAwait(false);
        var messageDataLength = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(messageDataLengthBuffer));

        // read the data buffer
        var messageData = new byte[messageDataLength];
        await EndpointHelper.FillBufferAsync(inboundStream, messageData, cancellationToken).ConfigureAwait(false);

        var message = new Message(correlationId, messageType, messageData);
        return message;
    }

    /// <summary>
    /// Reads data from the stream into the provided buffer until the buffer is filled.
    /// </summary>
    /// <param name="inboundStream">The stream to read from.</param>
    /// <param name="buffer">The buffer to fill with data.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    private static async Task FillBufferAsync(Stream inboundStream, Memory<byte> buffer, CancellationToken cancellationToken)
    {
        var totalBytes = 0;
        while ((totalBytes < buffer.Length) && !cancellationToken.IsCancellationRequested)
        {
            var bytesRead = await inboundStream.ReadAsync(
                buffer: buffer[totalBytes..buffer.Length],
                cancellationToken: cancellationToken).ConfigureAwait(false);
            if (bytesRead == 0)
            {
                break;
            }

            totalBytes += bytesRead;
        }

        if (totalBytes != buffer.Length)
        {
            throw new InvalidOperationException();
        }
    }

    public static async Task WriteMessageAsync(Stream outboundStream, Message message, CancellationToken token = default)
    {
        await EndpointHelper.WriteMessageAsync(outboundStream, message.CorrelationId, message.MessageType, message.MessageData, token).ConfigureAwait(false);
    }

    public static async Task WriteMessageAsync(Stream outboundStream, int correlationId, int messageType, byte[]? messageData, CancellationToken cancellationToken = default)
    {
        // write the correlation id
        var correlationIdBuffer = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(correlationId));
        await outboundStream.WriteAsync(correlationIdBuffer, cancellationToken).ConfigureAwait(false);

        // write the message type
        var messageTypeBuffer = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(messageType));
        await outboundStream.WriteAsync(messageTypeBuffer, cancellationToken).ConfigureAwait(false);

        // write the data length
        var messageLength = messageData?.Length ?? 0;
        var messageLengthBuffer = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(messageLength));
        await outboundStream.WriteAsync(messageLengthBuffer, cancellationToken).ConfigureAwait(false);

        // write the data buffer
        if (messageData != null)
        {
            await outboundStream.WriteAsync(messageData, cancellationToken).ConfigureAwait(false);
        }

        await outboundStream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }
}
