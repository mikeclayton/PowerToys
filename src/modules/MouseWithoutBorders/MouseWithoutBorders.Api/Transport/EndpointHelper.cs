﻿// Copyright (c) Microsoft Corporation
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
            var message = await channelReader.ReadAsync(cancellationToken);

            // write the message to the network stream for the server to pick up
            await EndpointHelper.WriteMessageAsync(outboundStream, message, cancellationToken);
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
            var message = await EndpointHelper.ReadMessageAsync(inboundStream, linkedCts.Token);
            if (message == null)
            {
                return;
            }

            // write the message to the "receive" channel for the caller to pick up
            await channelWriter.WriteAsync(message, linkedCts.Token);
        }
    }

    public static async Task<Message?> ReadMessageAsync(Stream inboundStream, CancellationToken cancellationToken)
    {
        // read the correlation id
        var correlationIdBuffer = new byte[4];
        var correlationIdBytesRead = await EndpointHelper.ReadExactlyAsync(inboundStream, correlationIdBuffer, correlationIdBuffer.Length, cancellationToken);
        if (correlationIdBytesRead != correlationIdBuffer.Length)
        {
            // client disconnected?
            return null;
        }

        var correlationId = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(correlationIdBuffer, 0));

        // read the message type
        var messageTypeBuffer = new byte[4];
        var messageTypeBytesRead = await EndpointHelper.ReadExactlyAsync(inboundStream, messageTypeBuffer, messageTypeBuffer.Length, cancellationToken);
        if (messageTypeBytesRead != messageTypeBuffer.Length)
        {
            // client disconnected?
            return null;
        }

        var messageType = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(messageTypeBuffer, 0));

        // read the data length
        var messageDataLengthBuffer = new byte[4];
        var messageDataLengthBytesRead = await EndpointHelper.ReadExactlyAsync(inboundStream, messageDataLengthBuffer, 4, cancellationToken);
        if (messageDataLengthBytesRead != messageDataLengthBuffer.Length)
        {
            // client disconnected?
            return null;
        }

        var messageDataLength = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(messageDataLengthBuffer, 0));

        // read the data buffer
        var messageData = new byte[messageDataLength];
        var messageDataBytesRead = await EndpointHelper.ReadExactlyAsync(inboundStream, messageData, messageDataLength, cancellationToken);
        if (messageDataBytesRead != messageData.Length)
        {
            // client disconnected?
            return null;
        }

        var message = new Message(correlationId, messageType, messageData);
        return message;
    }

    private static async Task<int> ReadExactlyAsync(Stream inboundStream, byte[] buffer, int count, CancellationToken cancellationToken)
    {
        int totalBytes = 0;
        while (totalBytes < count)
        {
            var bytesRead = await inboundStream.ReadAsync(
                buffer: buffer.AsMemory(totalBytes, count - totalBytes),
                cancellationToken: cancellationToken);
            if (bytesRead == 0)
            {
                break;
            }

            totalBytes += bytesRead;
        }

        return totalBytes;
    }

    public static async Task WriteMessageAsync(Stream outboundStream, Message message, CancellationToken token = default)
    {
        await EndpointHelper.WriteMessageAsync(outboundStream, message.CorrelationId, message.MessageType, message.MessageData, token);
    }

    public static async Task WriteMessageAsync(Stream outboundStream, int correlationId, int messageType, byte[]? messageData, CancellationToken cancellationToken = default)
    {
        // write the correlation id
        var correlationIdBuffer = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(correlationId));
        await outboundStream.WriteAsync(correlationIdBuffer, cancellationToken);

        // write the message type
        var messageTypeBuffer = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(messageType));
        await outboundStream.WriteAsync(messageTypeBuffer, cancellationToken);

        // write the data length
        var messageLength = messageData?.Length ?? 0;
        var messageLengthBuffer = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(messageLength));
        await outboundStream.WriteAsync(messageLengthBuffer, cancellationToken);

        // write the data buffer
        if (messageData != null)
        {
            await outboundStream.WriteAsync(messageData, cancellationToken);
        }

        await outboundStream.FlushAsync(cancellationToken);
    }
}
