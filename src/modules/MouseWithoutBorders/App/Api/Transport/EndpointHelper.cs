// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Threading;
using MouseWithoutBorders.Api.Models;
using MouseWithoutBorders.Api.Models.Messages;

#nullable enable

namespace MouseWithoutBorders.Api.Transport;

internal static class EndpointHelper
{
    public static async Task<Message?> ReadMessageAsync(Stream inboundStream, CancellationToken cancellationToken)
    {
        while (true)
        {
            // read the correlation id
            var correlationIdBuffer = new byte[4];
            var correlationIdBytesRead = await EndpointHelper.ReadExactlyAsync(inboundStream, correlationIdBuffer, correlationIdBuffer.Length, cancellationToken);
            if (correlationIdBytesRead != correlationIdBuffer.Length)
            {
                // client disconnected
                return null;
            }

            var correlationId = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(correlationIdBuffer, 0));

            // read the message type
            var messageTypeBuffer = new byte[4];
            var messageTypeBytesRead = await EndpointHelper.ReadExactlyAsync(inboundStream, messageTypeBuffer, messageTypeBuffer.Length, cancellationToken);
            if (messageTypeBytesRead != messageTypeBuffer.Length)
            {
                // client disconnected
                return null;
            }

            var messageType = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(messageTypeBuffer, 0));

            // read the data length
            var messageDataLengthBuffer = new byte[4];
            var messageDataLengthBytesRead = await EndpointHelper.ReadExactlyAsync(inboundStream, messageDataLengthBuffer, 4, cancellationToken);
            if (messageDataLengthBytesRead != messageDataLengthBuffer.Length)
            {
                // client disconnected
                return null;
            }

            var messageDataLength = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(messageDataLengthBuffer, 0));

            // read the data buffer
            var messageData = new byte[messageDataLength];
            var messageDataBytesRead = await EndpointHelper.ReadExactlyAsync(inboundStream, messageData, messageDataLength, cancellationToken);
            if (messageDataBytesRead != messageData.Length)
            {
                // client disconnected
                return null;
            }

            var message = new Message(correlationId, messageType, messageData);
            return message;
        }
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

    public static void WriteMessage(Stream outboundStream, Message message)
    {
        var joinableTaskContext = new JoinableTaskContext();
        var joinableTaskFactory = new JoinableTaskFactory(joinableTaskContext);
        joinableTaskFactory.Run(async () =>
        {
            await EndpointHelper.WriteMessageAsync(outboundStream, message.CorrelationId, message.MessageType, message.MessageData);
        });
    }

    public static void WriteMessage(Stream outboundStream, int correlationId, int messageType, byte[]? messageData)
    {
        var joinableTaskContext = new JoinableTaskContext();
        var joinableTaskFactory = new JoinableTaskFactory(joinableTaskContext);
        joinableTaskFactory.Run(async () =>
        {
            await EndpointHelper.WriteMessageAsync(outboundStream, correlationId, messageType, messageData);
        });
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
